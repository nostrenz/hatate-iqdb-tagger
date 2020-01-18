﻿using System.Windows;
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
		// Will turn true if the API is unreachable
		private bool unreachable = false;

		/*
		============================================
		Public
		============================================
		*/

		#region Public
		
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
		/// Query Hydrus and obtain file IDs from a list of tags.
		/// </summary>
		/// <param name="tags"></param>
		/// <param name="inbox"></param>
		/// <param name="archive"></param>
		/// <returns></returns>
		public async Task<JArray> SearchFiles(string[] tags, bool inbox=false, bool archive=false)
		{
			string json = await this.GetRequestAsync("/get_files/search_files?system_inbox=" + this.BoolToString(inbox) + "&system_archive=" + this.BoolToString(archive) + "&tags=" + System.Uri.EscapeDataString(
				Newtonsoft.Json.JsonConvert.SerializeObject(tags)
			));

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
		public async Task<JArray> GetFileMetadata(JArray fileIds)
		{
			string json = await this.GetRequestAsync("/get_files/file_metadata?file_ids=" + System.Uri.EscapeDataString(
				Newtonsoft.Json.JsonConvert.SerializeObject(fileIds.ToArray())
			));

			if (string.IsNullOrEmpty(json)) {
				return null;
			}
			
			dynamic parsed = JObject.Parse(json);
			
			return parsed.metadata;
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
				result.AddWarning("Hydrus: tags not sent - please select a tag service in Settings > Hydrus API");

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
		/// Send a URL to Hydrus.
		/// </summary>
		public async Task<bool> SendUrl(Result result)
		{
			if (result.Url == null) {
				result.AddWarning("Hydrus: URL not sent - no URL found for this file");

				return false;
			}

			string tagsPart = "";

			if (Settings.Default.SendUrlWithTags && result.HasTags && !string.IsNullOrEmpty(Settings.Default.HydrusTagService)) {
				tagsPart = @",
				""service_names_to_tags"" : {
					""" + Settings.Default.HydrusTagService + @""" : [" + this.TagsListToString(result.Tags) + @"]
				}";
			}

			string postData = @"{
				""url"" : """ + result.Url + "\"" + tagsPart + @"
			}";

			return await this.PostRequestAsync("/add_urls/add_url", postData) != null;
		}

		/// <summary>
		/// Import a file into Hydrus.
		/// http://hydrusnetwork.github.io/hydrus/help/client_api.html#add_files_add_file
		/// </summary>
		public async Task<string> ImportFile(Result result)
		{
			string postData = (@"{""path"" : """ + result.ImagePath + @"""}").Replace('\\', '/');
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
			} else {
				return data.hash;
			}

			return null;
		}

		/// <summary>
		/// Retrieve a file's thumbnail and store it in the thumbs folder.
		/// </summary>
		/// <param name="fileId"></param>
		/// <param name="thumbsDir"></param>
		/// <returns></returns>
		public Task<string> DownloadThumbnailAsync(string fileId, string thumbsDir)
		{
			return Task.Run(() => this.DownloadThumbnail(fileId, thumbsDir));
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
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Settings.Default.HydrusApiHost + ':' + Settings.Default.HydrusApiPort + route);

			request.Headers.Add("Hydrus-Client-API-Access-Key: " + Settings.Default.HydrusApiAccessKey);

			return request;
		}

		private string GetRequest(string route)
		{
			string response = null;
			HttpWebRequest request = this.CreateRequest(route);
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
			byte[] data = System.Text.Encoding.ASCII.GetBytes(postData);
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
		/// <param name="fileId"></param>
		/// <returns></returns>
		private string DownloadThumbnail(string fileId, string thumbsDir)
		{
			HttpWebRequest request = this.CreateRequest("/get_files/thumbnail?file_id=" + fileId);
			request.AutomaticDecompression = DecompressionMethods.GZip;

			Directory.CreateDirectory(thumbsDir);
			string filePath = thumbsDir + "hydrus-file-" + fileId + ".jpg";

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
				str += "\"" + tags[i].Namespaced + "\"";

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

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		public bool Unreachable
		{
			get { return this.unreachable; }
		}
	}
}