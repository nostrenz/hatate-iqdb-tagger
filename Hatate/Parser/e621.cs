using System.Net;
using Newtonsoft.Json.Linq;

namespace Hatate.Parser
{
	class e621 : Page, IParser
	{
		/*
		============================================
		Public
		============================================
		*/

		/// <summary>
		/// For e621 we'll use the API instead of parsing the HTML page.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public new bool FromUrl(string url)
		{
			string postId = url.Substring(url.LastIndexOf('/') + 1);

			if (postId == null) {
				return false;
			}

			string jsonUrl = "https://e621.net/posts/" + postId + ".json";

			using (WebClient webClient = new WebClient()) {
				webClient.Headers.Add("User-Agent", USER_AGENT);

				string json = null;

				try {
					json = webClient.DownloadString(jsonUrl);
				} catch (WebException webException) {
					HttpWebResponse response = (HttpWebResponse)webException.Response;

					if  (response.StatusCode == HttpStatusCode.NotFound) {
						this.unavailable = true;
					}

					return false;
				} catch {
					return false;
				}
				
				if (string.IsNullOrWhiteSpace(json)) {
					return false;
				}

				dynamic parsed = JObject.Parse(json);

				JObject post = parsed.post;
				JObject postFile = post.GetValue("file").ToObject<JObject>();
				JObject postTags = post.GetValue("tags").ToObject<JObject>();
			
				string rating = post.GetValue("rating").ToString();
				string width = postFile.GetValue("width").ToString();
				string height = postFile.GetValue("height").ToString();
				string size = postFile.GetValue("size").ToString();

				JArray postTagsGeneral = postTags.GetValue("general").ToObject<JArray>();
				JArray postTagsCopyright = postTags.GetValue("copyright").ToObject<JArray>();
				JArray postTagsArtist = postTags.GetValue("artist").ToObject<JArray>();
				JArray postTagsMeta = postTags.GetValue("meta").ToObject<JArray>();

				this.AddTagsFromArray(postTagsGeneral);
				this.AddTagsFromArray(postTagsCopyright, "series");
				this.AddTagsFromArray(postTagsArtist, "creator");
				this.AddTagsFromArray(postTagsMeta, "meta");

				long.TryParse(size, out this.size);
				int.TryParse(width, out this.width);
				int.TryParse(height, out this.height);

				this.full = postFile.GetValue("url").ToString();

				switch (rating) {
					case "s": this.rating = "Safe"; break;
					case "q": this.rating = "Questionable"; break;
					case "e": this.rating = "Explicit"; break;
				}
			}

			return true;
		}

		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			// Parsing is done by FromUrl()
			return true;
		}

		/*
		============================================
		Private
		============================================
		*/

		private void AddTagsFromArray(JArray tags, string nameSpace = null)
		{
			foreach (JToken jToken in tags) {
				string tag = jToken.Value<string>();

				if (string.IsNullOrEmpty(tag)) {
					continue;
				}

				tag = tag.Replace('_', ' ');

				this.AddTag(tag, nameSpace);
			}
		}
	}
}
