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

			using (WebClient webClient = new WebClient()) {
				webClient.Headers.Add("User-Agent", this.UserAgent);

				string json = null;

				try {
					json = webClient.DownloadString("https://e621.net/posts/" + postId + ".json");
				} catch (WebException webException) {
					HttpWebResponse response = (HttpWebResponse)webException.Response;

					if  (response.StatusCode == HttpStatusCode.NotFound) {
						this.unavailable = true;
					}

					return false;
				} catch {
					return false;
				}

				return this.ParseJson(json);
			}
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

		private bool ParseJson(string json)
		{
            if (string.IsNullOrWhiteSpace(json)) {
                return false;
            }

            dynamic parsed = JObject.Parse(json);
            JObject post = parsed.post;

            if (post == null) {
                return false;
            }

            JObject postFile = post.GetValue("file").ToObject<JObject>();
            JObject postTags = post.GetValue("tags").ToObject<JObject>();

            JToken rating = post.GetValue("rating");
            JToken width = postFile.GetValue("width");
            JToken height = postFile.GetValue("height");
            JToken size = postFile.GetValue("size");
            JToken full = postFile.GetValue("url");

            JToken postTagsGeneral = postTags.GetValue("general");
            JToken postTagsCopyright = postTags.GetValue("copyright");
            JToken postTagsArtist = postTags.GetValue("artist");
            JToken postTagsMeta = postTags.GetValue("meta");
			JToken postTagsSpecies = postTags.GetValue("species");

            this.AddTagsFromArray(postTagsGeneral);
            this.AddTagsFromArray(postTagsCopyright, "series");
            this.AddTagsFromArray(postTagsArtist, "creator");
            this.AddTagsFromArray(postTagsMeta, "meta");
			this.AddTagsFromArray(postTagsSpecies, "species");

            if (size != null) {
                long.TryParse(size.ToString(), out this.size);
            }

            if (width != null && height != null) {
                int.TryParse(width.ToString(), out this.width);
                int.TryParse(height.ToString(), out this.height);
            }

            if (full != null) {
                this.full = full.ToString();
            }

            if (rating != null) {
                switch (rating.ToString())
                {
                    case "s": this.rating = "Safe"; break;
                    case "q": this.rating = "Questionable"; break;
                    case "e": this.rating = "Explicit"; break;
                }
            }

			return true;
        }

		private void AddTagsFromArray(JToken tagsToken, string nameSpace = null)
		{
            if (tagsToken == null) {
                return;
            }

            JArray tags = tagsToken.ToObject<JArray>();

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
