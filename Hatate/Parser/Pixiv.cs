using System.Net;
using Newtonsoft.Json.Linq;

namespace Hatate.Parser
{
	class Pixiv : Page, IParser
	{
		/*
		============================================
		Public
		============================================
		*/

		/// <summary>
		/// For Pixiv we'll use the API instead of parsing the HTML page.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public new bool FromUrl(string url)
		{
			string illustId = null;

			// Extract illust ID from the URL
			if (url.Contains("/artworks/")) {
				illustId = url.Substring(url.LastIndexOf('/') + 1);
			} else if (url.Contains("illust_id=")) {
				illustId = url.Substring(url.LastIndexOf('=') + 1);
			}

			if (illustId == null) {
				return false;
			}

			string jsonUrl = "https://www.pixiv.net/ajax/illust/" + illustId;

			using (WebClient webClient = new WebClient()) {
				// By setting the language to English the Pixiv API will return tags with english translations
				webClient.Headers.Add("Accept-Language", "en-us;q=0.5,en;q=0.3");

				string json = null;

				try {
					json = webClient.DownloadString(jsonUrl);
				} catch {
					return false;
				}
				
				if (string.IsNullOrWhiteSpace(json)) {
					return false;
				}

				dynamic data = JObject.Parse(json);

				if (data == null || data.body == null || data.body.tags == null || data.body.tags.tags == null) {
					return false;
				}

				foreach (dynamic tag in data.body.tags.tags) {
					if (tag == null) {
						continue;
					}

					if (tag.translation != null && tag.translation.en != null) {
						this.AddTag((string)tag.translation.en);
					} else if (tag.romaji != null) {
						this.AddTag((string)tag.romaji);
					} else if (tag.tag != null) {
						this.AddTag((string)tag.tag);
					}
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
	}
}
