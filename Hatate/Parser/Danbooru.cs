using System.Net;
using Newtonsoft.Json.Linq;

namespace Hatate.Parser
{
	class Danbooru : Page, IParser
	{
		/// <summary>
		/// For Danbooru we'll use the API instead of parsing the HTML page.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public new bool FromUrl(string url)
		{
			string postId = url.Substring(url.LastIndexOf('/') + 1);

			if (postId == null) {
				return false;
			}

			string jsonUrl = "https://danbooru.donmai.us/posts/" + postId + ".json";

			using (WebClient webClient = new WebClient()) {
				webClient.Headers.Add("User-Agent", "Hatate/1.0");

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

				string rating = parsed.GetValue("rating").ToString();
				string width = parsed.GetValue("image_width").ToString();
				string height = parsed.GetValue("image_height").ToString();
				string size = parsed.GetValue("file_size").ToString();
				string deleted = parsed.GetValue("is_deleted").ToString(); // "true" / "false"
				string banned = parsed.GetValue("is_banned").ToString(); // "true" / "false"

				string tagStringGeneral = parsed.GetValue("tag_string_general").ToString();
				string tagStringCharacter = parsed.GetValue("tag_string_character").ToString();
				string tagStringCopyright = parsed.GetValue("tag_string_copyright").ToString();
				string tagStringArtist = parsed.GetValue("tag_string_artist").ToString();
				string tagStringMeta = parsed.GetValue("tag_string_meta").ToString();

				long.TryParse(size, out this.size);
				int.TryParse(width, out this.width);
				int.TryParse(height, out this.height);

				this.full = parsed.GetValue("file_url").ToString();
				this.source = parsed.GetValue("source").ToString();

				switch (rating) {
					case "g": this.rating = "General"; break;
					case "s": this.rating = "Safe"; break;
					case "q": this.rating = "Questionable"; break;
					case "e": this.rating = "Explicit"; break;
				}

				if (deleted == "true" || banned == "true") {
					this.unavailable = true;
				}

				this.AddTagsFromString(tagStringGeneral);
				this.AddTagsFromString(tagStringCharacter, "character");
				this.AddTagsFromString(tagStringCopyright, "series");
				this.AddTagsFromString(tagStringArtist, "creator");
				this.AddTagsFromString(tagStringMeta, "meta");
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

		private void AddTagsFromString(string tagsString, string nameSpace = null)
		{
			if (string.IsNullOrWhiteSpace(tagsString)) {
				return;
			}

			string[] tags = tagsString.Split(' ');

			foreach (string tag in tags) {
				this.AddTag(tag.Replace('_', ' '), nameSpace);
			}
		}
	}
}
