using System.Windows;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Hatate.Properties;
using Newtonsoft.Json.Linq;
using Directory = System.IO.Directory;

namespace Hatate
{
	public class HydrusApi
	{
		const int METADATA_BATCH_SIZE = 1000;

		// Will turn true if the API is unreachable
		private bool unreachable = false;

		private ushort clientVersion = 0;
		private ushort apiVersion = 0;

		/*
		============================================
		Public
		============================================
		*/

		#region Public
		
		/// <summary>
		/// Retrieves client and API versions.
		/// </summary>
		public async Task RetrieveVersions()
		{
			string json = await this.GetRequestAsync("/api_version");

			if (string.IsNullOrEmpty(json)) {
				return;
			}

			dynamic versions = JObject.Parse(json);

			this.clientVersion = (ushort)versions.hydrus_version;
			this.apiVersion = (ushort)versions.version;
		}

		/// <summary>
		/// Get all the available tag services.
		/// </summary>
		/// <returns></returns>
		public async Task<JArray> GetTagServices()
		{
			string json = await this.GetRequestAsync("/add_tags/get_tag_services");

			if (string.IsNullOrEmpty(json)) {
				return null;
			}

			dynamic services = JObject.Parse(json);
			
			return services.local_tags;
		}

		/// <summary>
		/// Get all the available services.
		/// </summary>
		/// <returns></returns>
		public async Task<JObject> GetServices()
		{
			string json = await this.GetRequestAsync("/get_services");

			if (string.IsNullOrEmpty(json)) {
				return null;
			}

			return JObject.Parse(json);
		}

		/// <summary>
		/// Query Hydrus and obtain file IDs from a list of tags.
		/// </summary>
		/// <param name="tags"></param>
		/// <param name="inbox"></param>
		/// <param name="archive"></param>
		/// <returns></returns>
		public async Task<JArray> SearchFiles(string[] tags, bool inbox=false, bool archive=false, string tagServiceKey = null, string fileServiceKey=null)
		{
			string route = "/get_files/search_files?";

			route += "system_inbox=" + this.BoolToString(inbox);
			route += "&system_archive=" + this.BoolToString(archive);

			if (this.SearchFilesSupportsServiceArguments) {
				if (tagServiceKey != null) {
					route += "&tag_service_key=" + tagServiceKey;
				}

				if (fileServiceKey != null) {
					route += "&file_service_key=" + fileServiceKey;
				}
			}

			route += "&tags=" + System.Uri.EscapeDataString(
				Newtonsoft.Json.JsonConvert.SerializeObject(tags)
			);

			string json = await this.GetRequestAsync(route);

			if (string.IsNullOrEmpty(json)) {
				return null;
			}

			dynamic parsed = JObject.Parse(json);
			
			return parsed.file_ids;
		}

		/// <summary>
		/// Get a file's metadata from its ID.
		/// </summary>
		/// <param name="fileIds"></param>
		/// <returns></returns>
		public async Task<JArray> GetFilesMetadata(JArray fileIds)
		{
			// We're limited by how long the URL can be as EscapeDataString() won't accept to work with strings longer than 32766 chars
			// So we'll query the metadata by batches of 1000

			if (fileIds.Count <= METADATA_BATCH_SIZE) {
				return await this.GetBatchMetadata(fileIds);
			}
			
			JArray metadata = new JArray();
			JArray batch = new JArray();

			while (fileIds.Count > 0) {
				// End now
				if (fileIds.Count <= METADATA_BATCH_SIZE) {
					metadata.Merge(await this.GetBatchMetadata(fileIds));

					return metadata;
				}

				// Fill the batch while emptying fileIds
				batch.Add(fileIds[0]);
				fileIds.Remove(fileIds[0]);

				// Batch is ready to be used
				if (batch.Count >= METADATA_BATCH_SIZE) {
					metadata.Merge(await this.GetBatchMetadata(batch));
					batch.Clear();
				}
			}

			return metadata;
		}

		/// <summary>
		/// Send tags to Hydrus for a file.
		/// </summary>
		/// <param name="fileHash"></param>
		/// <param name="tags"></param>
		public async Task<bool> SendTagsForFile(Result result)
		{
			// Missing tag service
			if (string.IsNullOrEmpty(Settings.Default.HydrusTagService)) {
				result.AddWarning("Hydrus: tags not sent - please go to Settings > Hydrus API and select a tag service");

				return false;
			}

			string postData = (@"{
				""hash"": """ + result.Local.Hash + @""",
				""service_names_to_tags"": {
					""" + Settings.Default.HydrusTagService + @""": [" + this.TagsListToString(result.Tags) + @"]
				}
			}");

			return await this.PostRequestAsync("/add_tags/add_tags", postData) != null;
		}

		/// <summary>
		/// Send a single URL to Hydrus.
		/// </summary>
		public async Task<bool> SendUrl(Result result, string url)
		{
			if (url == null) {
				result.AddWarning("Hydrus: URL not sent - no URL found for this file");

				return false;
			}

			string tagsPart = "";

			if (Settings.Default.SendUrlWithTags && result.HasTags) {
				// Missing tag service
				if (string.IsNullOrEmpty(Settings.Default.HydrusTagService)) {
					result.AddWarning("Hydrus: tags not sent alongside the URL - please go to Settings > Hydrus API and select a tag service or disable the \"Send tags alongside a URL\" option.");

					return false;
				}

				tagsPart = @",
				""service_names_to_tags"" : {
					""" + Settings.Default.HydrusTagService + @""" : [" + this.TagsListToString(result.Tags) + @"]
				}";
			}

			string postData = @"{
				""url"" : """ + url + "\"" + tagsPart + @"
			}";

			return await this.PostRequestAsync("/add_urls/add_url", postData) != null;
		}

		/// <summary>
		/// Import a file into Hydrus.
		/// http://hydrusnetwork.github.io/hydrus/help/client_api.html#add_files_add_file
		/// </summary>
		public async Task<string> ImportFile(Result result)
		{
			string postData = @"{""path"" : """ + result.ImagePath.Replace('\\', '/') + @"""}";
			string response = await this.PostRequestAsync("/add_files/add_file", postData);

			if (string.IsNullOrEmpty(response)) {
				return null;
			}

			dynamic data = JObject.Parse(response);
			int status = (int)data.status;

			if (status == 3) {
				result.AddWarning("Hydrus: not imported - previously deleted");
			} else if (status == 4) {
				result.AddWarning("Hydrus: not imported - failed to import");
			} else if (status == 7) {
				result.AddWarning("Hydrus: not imported - vetoed");
			}

			return data.hash;
		}

		/// <summary>
		/// Retrieve a file's thumbnail and store it in the thumbs folder.
		/// </summary>
		/// <param name="hydrusMetadata"></param>
		/// <param name="thumbsDir"></param>
		/// <returns></returns>
		public Task<string> DownloadThumbnailAsync(HydrusMetadata hydrusMetadata, string thumbsDir)
		{
			return Task.Run(() => this.DownloadThumbnail(hydrusMetadata, thumbsDir));
		}

		/// <summary>
		/// Associate a URL with a file.
		/// </summary>
		/// <param name="fileHash"></param>
		/// <param name="url"></param>
		/// <returns></returns>
		public async Task<bool> AssociateUrl(string fileHash, string url)
		{
			string postData = @"{""url_to_add"": """ + url + @""", ""hash"": """ + fileHash + @"""}";

			return await this.PostRequestAsync("/add_urls/associate_url", postData) != null;
		}

		/// <summary>
		/// Get client pages.
		/// </summary>
		/// <returns></returns>
		public async Task<JArray> GetPages()
		{
			string json = await this.GetRequestAsync("/manage_pages/get_pages");

			if (string.IsNullOrEmpty(json)) {
				return null;
			}

			dynamic pages = JObject.Parse(json);

			if (pages == null || pages.pages == null) {
				return null;
			}
			
			// Returns pages contained in the top page
			return pages.pages.pages;
		}

		/// <summary>
		/// Focus a page by its page key.
		/// </summary>
		/// <param name="pageKey"></param>
		/// <returns></returns>
		public async Task<bool> FocusPage(string pageKey)
		{
			string postData = @"{""page_key"": """ + pageKey + @"""}";

			return await this.PostRequestAsync("/manage_pages/focus_page", postData) != null;
		}

		/// <summary>
		/// Try to get a page with a given name, return its page key if found.
		/// </summary>
		/// <param name="pageName"></param>
		/// <returns></returns>
		public async Task<string> GetPageNamed(string pageName, bool focus=false)
		{
			JArray pages = await App.hydrusApi.GetPages();

			if (pages == null || pages.Count < 1) {
				return null;
			}

			foreach (JToken jToken in pages) {
				JObject page = jToken.Value<JObject>();
				string name = page.GetValue("name").ToString();

				if (name != pageName) {
					continue;
				}

				string pageKey = page.GetValue("page_key").ToString();

				if (focus && pageKey != null) {
					await this.FocusPage(pageKey);
				}

				return pageKey;
			}

			return null;
		}

		/// <summary>
		/// Display a single file in a page.
		/// </summary>
		/// <param name="pageKey"></param>
		/// <param name="fileIds"></param>
		/// <returns></returns>
		public async Task<bool> AddFileToPage(string pageKey, string fileHash)
		{
			string postData = @"{""page_key"": """ + pageKey + @""", ""hashes"": [""" + fileHash + @"""]}";

			return await this.PostRequestAsync("/manage_pages/add_files", postData) != null;
		}

		/// <summary>
		/// Display multiple files in a page.
		/// </summary>
		/// <param name="pageKey"></param>
		/// <param name="fileIds"></param>
		/// <returns></returns>
		public async Task<bool> AddFilesToPage(string pageKey, List<string> fileHashes)
		{
			string hashes = "";

			for (int i = 0; i < fileHashes.Count; i++) {
				hashes += @"""" + fileHashes[i] + @"""";

				if (i < fileHashes.Count - 1) {
					hashes += ", ";
				}
			}

			string postData = @"{""page_key"": """ + pageKey + @""", ""hashes"": [" + hashes + @"]}";

			return await this.PostRequestAsync("/manage_pages/add_files", postData) != null;
		}

		public void ShowApiUnreachableMessage(string exceptionMessage)
		{
			this.unreachable = true;

			MessageBox.Show("Unable to call the Hydrus API. Please verify that Hydrus is running with a configured local files API.\n\n" + exceptionMessage);
		}

		public void ResetUnreachableFlag()
		{
			this.unreachable = false;
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		private HttpWebRequest CreateRequest(string route)
		{
			// Ensure compatibility with previous versions
			if (!string.IsNullOrEmpty(Settings.Default.HydrusApiPort)) {
				Settings.Default.HydrusApiHost += ':' + Settings.Default.HydrusApiPort;
				Settings.Default.HydrusApiPort = null;
				Settings.Default.Save();
			}

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Settings.Default.HydrusApiHost + route);

			request.Headers.Add("Hydrus-Client-API-Access-Key: " + Settings.Default.HydrusApiAccessKey);

			return request;
		}

		private string GetRequest(string route)
		{
			string response = null;
			HttpWebRequest request;

			try {
				request = this.CreateRequest(route);
			} catch (System.UriFormatException) {
				return null;
			}

			request.AutomaticDecompression = DecompressionMethods.GZip;

			try {
				using (HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse()) {
					if (webResponse.StatusCode != HttpStatusCode.OK) {
						return null;
					}

					using (Stream stream = webResponse.GetResponseStream())
					using (StreamReader reader = new StreamReader(stream)) {
						response = reader.ReadToEnd();
					}
				}
			} catch (WebException e) {
				this.ShowApiUnreachableMessage(e.Message);
			}

			return response;
		}

		private string PostRequest(string route, string postData)
		{
			HttpWebRequest request = this.CreateRequest(route);
			byte[] data = System.Text.Encoding.UTF8.GetBytes(postData);
			string response = null;

			request.Method = "POST";
			request.ContentType = "application/json";
			request.ContentLength = data.Length;

			try {
				using (var stream = request.GetRequestStream()) {
					stream.Write(data, 0, data.Length);
				}

				HttpWebResponse webResponse = (HttpWebResponse)request.GetResponse();

				if (webResponse.StatusCode != HttpStatusCode.OK) {
					return null;
				}

				response = new StreamReader(webResponse.GetResponseStream()).ReadToEnd();
			} catch (WebException e) {
				this.ShowApiUnreachableMessage(e.Message);
			}

			return response;
		}

		/// <summary>
		/// Write a thumbnail from Hydrus into the thumbs folder.
		/// </summary>
		/// <param name="hydrusMetadata"></param>
		/// <returns></returns>
		private string DownloadThumbnail(HydrusMetadata hydrusMetadata, string thumbsDir)
		{
			HttpWebRequest request = this.CreateRequest("/get_files/thumbnail?file_id=" + hydrusMetadata.FileId);
			request.AutomaticDecompression = DecompressionMethods.GZip;

			Directory.CreateDirectory(thumbsDir);
			string filePath = thumbsDir + "hydrus-file-" + hydrusMetadata.FileId + "." + hydrusMetadata.Extension;

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
				using (var fileStream = File.Create(filePath)) {
					response.GetResponseStream().CopyTo(fileStream);
				}
			}

			return filePath;
		}

		private Task<string> GetRequestAsync(string route)
		{
			return Task.Run(() => this.GetRequest(route));
		}

		private Task<string> PostRequestAsync(string route, string postData)
		{
			return Task.Run(() => this.PostRequest(route, postData));
		}

		private string TagsListToString(List<Tag> tags)
		{
			string str = "";

			for (int i = 0; i < tags.Count; i++) {
				str += "\"" + tags[i].Namespaced.Replace(@"\", @"\\") + "\"";

				if (i < tags.Count - 1) {
					str += ", ";
				}
			}

			return str;
		}

		private string BoolToString(bool value)
		{
			return value ? "true" : "false";
		}

		private async Task<JArray> GetBatchMetadata(JArray fileIds)
		{
			string param = Newtonsoft.Json.JsonConvert.SerializeObject(fileIds.ToArray());

			// EscapeDataString() won't accept to work with a string longer than 32766 chars
			if (param.Length > 32766) {
				return null;
			}

			try {
				param = System.Uri.EscapeDataString(param);
			} catch (System.UriFormatException) {
				return null;
			}

			string json = await this.GetRequestAsync("/get_files/file_metadata?file_ids=" + param);

			if (string.IsNullOrEmpty(json)) {
				return null;
			}

			dynamic parsed = JObject.Parse(json);

			return parsed.metadata;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		public ushort ClientVersion
		{
			get { return this.clientVersion; }
		}

		public ushort ApiVersion
		{
			get { return this.apiVersion; }
		}

		public bool Unreachable
		{
			get { return this.unreachable; }
		}

		/// <summary>
		/// Returns true if the /get_files/search_files API supports the following arguments:
		/// file_service_name, file_service_key, tag_service_name and tag_service_key.
		/// </summary>
		/// <returns></returns>
		public bool SearchFilesSupportsServiceArguments
		{
			get { return this.apiVersion >= 19; }
		}
	}
}
