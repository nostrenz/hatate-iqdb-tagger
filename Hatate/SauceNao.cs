using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Hatate
{
	class SauceNao
	{
		private List<Match> matches = new List<Match>();

		/*
		============================================
		Public
		============================================
		*/

		#region Public

		public async Task SearchFile(string filePath)
		{
			string response = await this.SearchImage(filePath);

			this.ParseResponseHtml(response);
		}

		#endregion Public

		/*
		============================================
		Private
		============================================
		*/

		#region Private

		/// <summary>
		/// Search an image on SauceNAO and return the HTML response.
		/// </summary>
		/// <param name="filePath"></param>
		private async Task<string> SearchImage(string filePath)
		{
			FileStream fs = null;

			try {
				fs = new FileStream(filePath, FileMode.Open);
			} catch (IOException) {
				return null; // May happen if the file is in use
			}

			MultipartFormDataContent form = new MultipartFormDataContent();
			form.Add(new StreamContent(fs), "file", "image.jpg");

			HttpClient httpClient = new HttpClient(new HttpClientHandler());
			HttpResponseMessage response = await httpClient.PostAsync("https://saucenao.com/search.php", form);

			fs.Close();
			fs.Dispose();

			return await response.Content.ReadAsStringAsync();
		}

		/// <summary>
		/// Parse HTML responded by SauceNAO.
		/// </summary>
		private void ParseResponseHtml(string html)
		{
			Supremes.Nodes.Document doc = null;

			try {
				doc = Supremes.Dcsoup.Parse(html, "utf-8");
			} catch {
				return;
			}

			Supremes.Nodes.Elements results = doc.Select("#middle > .result");

			if (results.Count < 1) {
				return;
			}

			foreach (Supremes.Nodes.Element result in results) {
				Supremes.Nodes.Elements sourceLinks = result.Select(".resultmiscinfo > a");

				if (sourceLinks.Count < 1) {
					continue;
				}

				Supremes.Nodes.Element resultimage = result.Select(".resultimage img").First;
				Supremes.Nodes.Element resultsimilarityinfo = result.Select(".resultsimilarityinfo").First;

				foreach (Supremes.Nodes.Element sourceLink in sourceLinks) {
					Match match = new Match();

					match.Url = sourceLink.Attr("href");
					match.PreviewUrl = resultimage.Attr("src");
					match.Similarity = this.ParseSimilarity(resultsimilarityinfo.Text);

					if (match.Url.Contains("danbooru.donmai.us")) {
						match.Source = IqdbApi.Enums.Source.Danbooru;
					} else if (match.Url.Contains("gelbooru.com")) {
						match.Source = IqdbApi.Enums.Source.Gelbooru;
					} else {
						// Unrecognized source
						continue;
					}

					this.matches.Add(match);
				}
			}
		}

		/// <summary>
		/// Parse similarity text.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private byte ParseSimilarity(string text)
		{
			text = text.Trim();

			if (text.Contains(".")) {
				text = text.Substring(0, text.IndexOf('.'));
			} else {
				text = text.Replace("%", "");
			}

			byte similarity = 0;
			byte.TryParse(text, out similarity);

			return similarity;
		}

		#endregion Private

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public ImmutableList<Match> Matches
		{
			get { return ImmutableList.Create(this.matches.ToArray()); }
		}

		#endregion Accessor
	}
}
