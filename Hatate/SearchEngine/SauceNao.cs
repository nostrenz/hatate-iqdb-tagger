using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Hatate.Properties;

namespace Hatate
{
	class SauceNao
	{
		private const string BLOCKED_GIF = "images/static/blocked.gif";

		private List<Match> matches = new List<Match>();
		private string uploadedImageUrl = null;
		private bool dailyLimitExceeded = false;

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
			FileStream fs;

			try {
				fs = new FileStream(filePath, FileMode.Open);
			} catch (IOException) {
				return null; // May happen if the file is in use
			}

			MultipartFormDataContent form = new MultipartFormDataContent();
			form.Add(new StreamContent(fs), "file", "image.jpg");

			string url = "https://saucenao.com/search.php";

			// Add API key if available
			if (!string.IsNullOrWhiteSpace(Settings.Default.SauceNaoApiKey)) {
				url += "?api_key=" + Settings.Default.SauceNaoApiKey;
			}

			HttpClient httpClient = new HttpClient(new HttpClientHandler());
			HttpResponseMessage response = await httpClient.PostAsync(url, form);

			fs.Close();
			fs.Dispose();

			return await response.Content.ReadAsStringAsync();
		}

		/// <summary>
		/// Parse HTML responded by SauceNAO.
		/// </summary>
		private void ParseResponseHtml(string html)
		{
			Supremes.Nodes.Document doc;

			try {
				doc = Supremes.Dcsoup.Parse(html, "utf-8");
			} catch {
				return;
			}

			Supremes.Nodes.Element yourImage = doc.Select("#yourimage > a > img").First;
			Supremes.Nodes.Elements results = doc.Select("#middle > .result");

			if (yourImage != null) {
				this.uploadedImageUrl = "https://saucenao.com/" + yourImage.Attr("src");
			}

			if (results.Count < 1) {
				// Check if search limit was exceeded
				Supremes.Nodes.Element strong = doc.Select("strong").First;

				if (strong != null && strong.Text == "Daily Search Limit Exceeded.") {
					this.dailyLimitExceeded = true;
				}

				return;
			}

			foreach (Supremes.Nodes.Element result in results) {
				Supremes.Nodes.Element resultcontent = result.Select(".resultcontent").First;

				if (resultcontent == null) {
					continue;
				}

				List<Tag> resultTags = new List<Tag>();

				Supremes.Nodes.Elements sourceLinks = result.Select(".resultmiscinfo > a");
				Supremes.Nodes.Element resultimage = result.Select(".resultimage img").First;
				Supremes.Nodes.Element resultsimilarityinfo = result.Select(".resultsimilarityinfo").First;
				Supremes.Nodes.Element originalSourceLink = resultcontent.Select(".resultcontentcolumn a").First;

				// Get preview image
				string previewUrl = resultimage.Attr("src");

				if (previewUrl == BLOCKED_GIF) {
					previewUrl = resultimage.Attr("data-src");
				}

				// Get tags provided by SauceNAO
				if (this.ShouldGetSauceNaoResultTags) {
					Supremes.Nodes.Element resulttitle = resultcontent.Select(".resulttitle").First;
					Supremes.Nodes.Elements resultcontentcolumns = resultcontent.Select(".resultcontentcolumn");
					Supremes.Nodes.Elements originalSourceLinks = resultcontent.Select(".resultcontentcolumn a");

					// Title tag
					if (resulttitle != null) {
						string titleText = resulttitle.Text;
						string nameSpace = Settings.Default.SauceNao_TagNamespace_Title;

						if (titleText.StartsWith("Creator:")) {
							titleText = titleText.Replace("Creator:", "");
							nameSpace = Settings.Default.SauceNao_TagNamespace_Creator;
						}

						resultTags.Add(new Tag(titleText.Trim(), nameSpace) { Source = Enum.TagSource.SearchEngine });
					}

					foreach (Supremes.Nodes.Element resultcontentcolumn in resultcontentcolumns) {
						string contentHtml = resultcontentcolumn.Html;

						int materialPos = contentHtml.IndexOf("<strong>Material: </strong>");
						int charactersPos = contentHtml.IndexOf("<strong>Characters: </strong>");

						if (materialPos >= 0) {
							string materialHtml = contentHtml.Substring(materialPos);
							materialHtml = materialHtml.Replace("<strong>Material: </strong>", "");
							materialHtml = materialHtml.Replace("<br>", "");

							resultTags.Add(new Tag(materialHtml.Trim(), Settings.Default.SauceNao_TagNamespace_Material) { Source = Enum.TagSource.SearchEngine });
						}

						if (charactersPos >= 0) {
							string charactersHtml = contentHtml.Replace("<strong>Characters: </strong>", "").Trim();
							charactersHtml = charactersHtml.Replace("<br>", "");
							string[] characterNames = charactersHtml.Split('\n');

							foreach (string characterName in characterNames) {
								if (!string.IsNullOrWhiteSpace(characterName)) {
									resultTags.Add(new Tag(characterName.Trim(), Settings.Default.SauceNao_TagNamespace_Character) { Source = Enum.TagSource.SearchEngine });
								}
							}
						}
					}

					// Source links
					foreach (Supremes.Nodes.Element sourceLink in originalSourceLinks) {
						string url = sourceLink.Attr("href");

						// Not a pixiv URL
						if (!url.StartsWith("https://www.pixiv.net")) {
							continue;
						}

						int questionMarkIndex = url.IndexOf('?');

						// No query string
						if (questionMarkIndex < 1) {
							continue;
						}

						// Get query string
						string queryString = url.Substring(questionMarkIndex + 1);

						// Parse query string
						string[] parts = queryString.Split('&');

						foreach (string part in parts) {
							string[] keyValue = part.Split('=');

							string key = keyValue[0];
							string value = keyValue[1];

							if (key == "illust_id") {
								resultTags.Add(new Tag(value, Settings.Default.SauceNao_TagNamespace_PixivIllustId) { Source = Enum.TagSource.SearchEngine });
							} else if (key == "id") {
								resultTags.Add(new Tag(value, Settings.Default.SauceNao_TagNamespace_PixivMemberId) { Source = Enum.TagSource.SearchEngine });
								resultTags.Add(new Tag(sourceLink.Text, Settings.Default.SauceNao_TagNamespace_PixivMemberName) { Source = Enum.TagSource.SearchEngine });
							}
						}
					}
				}

				// There were no booru links for this result but we have the link to pixiv/etc
				if (sourceLinks.Count == 0 && originalSourceLink != null) {
					this.AddMatch(
						originalSourceLink.Attr("href"),
						previewUrl,
						resultsimilarityinfo.Text,
						resultTags
					);

					continue;
				}

				// We have booru links for this result
				foreach (Supremes.Nodes.Element sourceLink in sourceLinks) {
					this.AddMatch(
						sourceLink.Attr("href"),
						previewUrl,
						resultsimilarityinfo.Text,
						resultTags,
						originalSourceLink != null ? originalSourceLink.Attr("href") : null
					);
				}
			}
		}

		private void AddMatch(string url, string previewUrl, string similarity, List<Tag> resultTags, string sourceUrl=null)
		{
			Match match = new Match();

			match.Url = url;
			match.PreviewUrl = previewUrl;
			match.Similarity = this.ParseSimilarity(similarity);
			match.DetermineSourceFromUrl();

			if (match.PreviewUrl == BLOCKED_GIF) {
				match.PreviewUrl = null;
			}

			if (sourceUrl != null) {
				match.SourceUrl = sourceUrl;
			}

			foreach (Tag tag in resultTags) {
				match.Tags.Add(tag);
			}

			this.matches.Add(match);
		}

		/// <summary>
		/// Parse similarity text.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		private float ParseSimilarity(string text)
		{
			text = text.Replace("%", "").Trim();

			CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
			culture.NumberFormat.NumberDecimalSeparator = ".";

			try {
				return float.Parse(text, culture);
			} catch (System.Exception) {
				return 0;
			}
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

		public string UploadedImageUrl
		{
			get { return this.uploadedImageUrl; }
		}

		public bool DailyLimitExceeded
		{
			get { return this.dailyLimitExceeded; }
		}

		private bool ShouldGetSauceNaoResultTags
		{
			get
			{
				return (Settings.Default.SauceNao_TagNamespace_Title != null && !Settings.Default.SauceNao_TagNamespace_Title.StartsWith("-"))
					|| (Settings.Default.SauceNao_TagNamespace_Creator != null && !Settings.Default.SauceNao_TagNamespace_Creator.StartsWith("-"))
					|| (Settings.Default.SauceNao_TagNamespace_Material != null && !Settings.Default.SauceNao_TagNamespace_Material.StartsWith("-"))
					|| (Settings.Default.SauceNao_TagNamespace_Character != null && !Settings.Default.SauceNao_TagNamespace_Character.StartsWith("-"))
					|| (Settings.Default.SauceNao_TagNamespace_PixivIllustId != null && !Settings.Default.SauceNao_TagNamespace_PixivIllustId.StartsWith("-"))
					|| (Settings.Default.SauceNao_TagNamespace_PixivMemberId != null && !Settings.Default.SauceNao_TagNamespace_PixivMemberId.StartsWith("-"))
					|| (Settings.Default.SauceNao_TagNamespace_PixivMemberName != null && !Settings.Default.SauceNao_TagNamespace_PixivMemberName.StartsWith("-"));
			}
		}

		#endregion Accessor
	}
}
