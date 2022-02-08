using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using Newtonsoft.Json.Linq;
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
			bool useJsonApi = !string.IsNullOrWhiteSpace(Settings.Default.SauceNaoApiKey);
			string response = await this.SearchImage(filePath, useJsonApi);

			if (useJsonApi) {
				this.ParseJsonResponse(response);
			} else {
				this.ParseResponseHtml(response);
			}
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
		private async Task<string> SearchImage(string filePath, bool useJsonApi)
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
			if (useJsonApi) {
				url += "?api_key=" + Settings.Default.SauceNaoApiKey;
				url += "&output_type=2";
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
				if (this.ShouldGetTags) {
					Supremes.Nodes.Element resulttitle = resultcontent.Select(".resulttitle").First;
					Supremes.Nodes.Elements resultcontentcolumns = resultcontent.Select(".resultcontentcolumn");
					Supremes.Nodes.Elements originalSourceLinks = resultcontent.Select(".resultcontentcolumn a");

					// Title or creator tag
					if (resulttitle != null) {
						string titleText = resulttitle.Text;
						string nameSpace = Settings.Default.SauceNao_TagNamespace_Title;

						if (titleText.StartsWith("Creator:") && this.ShouldGetCreatorTag) {
							titleText = titleText.Replace("Creator:", "");
							nameSpace = Settings.Default.SauceNao_TagNamespace_Creator;

							resultTags.Add(new Tag(titleText.Trim(), nameSpace) { Source = Enum.TagSource.SearchEngine });
						} else if (this.ShouldGetTitleTag) {
							resultTags.Add(new Tag(titleText.Trim(), nameSpace) { Source = Enum.TagSource.SearchEngine });
						}
					}

					foreach (Supremes.Nodes.Element resultcontentcolumn in resultcontentcolumns) {
						string contentHtml = resultcontentcolumn.Html;

						// Get material tag
						if (this.ShouldGetMaterialTag) {
							int materialPos = contentHtml.IndexOf("<strong>Material: </strong>");

							if (materialPos >= 0) {
								string materialHtml = contentHtml.Substring(materialPos);
								materialHtml = materialHtml.Replace("<strong>Material: </strong>", "");
								materialHtml = materialHtml.Replace("<br>", "");

								resultTags.Add(new Tag(materialHtml.Trim(), Settings.Default.SauceNao_TagNamespace_Material) { Source = Enum.TagSource.SearchEngine });
							}
						}

						// Get character tags
						if (this.ShouldGetCharacterTag) {
							int charactersPos = contentHtml.IndexOf("<strong>Characters: </strong>");

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
								if (this.ShouldGetPixivIllustIdTag) {
									resultTags.Add(new Tag(value, Settings.Default.SauceNao_TagNamespace_PixivIllustId) { Source = Enum.TagSource.SearchEngine });
								}
							} else if (key == "id") {
								if (this.ShouldGetPixivMemberIdTag) {
									resultTags.Add(new Tag(value, Settings.Default.SauceNao_TagNamespace_PixivMemberId) { Source = Enum.TagSource.SearchEngine });
								}

								if (this.ShouldGetPixivMemberNameTag) {
									resultTags.Add(new Tag(sourceLink.Text, Settings.Default.SauceNao_TagNamespace_PixivMemberName) { Source = Enum.TagSource.SearchEngine });
								}
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

		/// <summary>
		/// Parse JSON responded by SauceNAO.
		/// </summary>
		/// <param name="json"></param>
		private void ParseJsonResponse(string json)
		{
			dynamic parsedJson = JObject.Parse(json);

			JObject header = parsedJson.header;
			JArray results = parsedJson.results;

			string queryImageDisplay = (string)header.GetValue("query_image_display");
			//string queryImage = (string)header.GetValue("query_image");

			if (!string.IsNullOrWhiteSpace(queryImageDisplay)) {
				this.uploadedImageUrl = "https://saucenao.com/" + queryImageDisplay;
			}

			foreach (JObject result in results) {
				List<Tag> resultTags = new List<Tag>();

				JObject header2 = (JObject)result.GetValue("header");
				JObject data = (JObject)result.GetValue("data");
				JArray extUrls = (JArray)data.GetValue("ext_urls");

				string similarity = (string)header2.GetValue("similarity");
				string thumbnail = (string)header2.GetValue("thumbnail");

				// Common
				string source = (string)data.GetValue("source");
				string title = (string)data.GetValue("title");
				string memberName = (string)data.GetValue("member_name");
				string memberId = (string)data.GetValue("member_id");

				if (this.ShouldGetTitleTag && !string.IsNullOrWhiteSpace(title)) resultTags.Add(new Tag(title, Settings.Default.SauceNao_TagNamespace_Title) { Source = Enum.TagSource.SearchEngine });

				// Pixiv
				string pixivId = (string)data.GetValue("pixiv_id");
				bool hasPixivId = !string.IsNullOrWhiteSpace(pixivId);

				if (hasPixivId) {
					if (this.ShouldGetPixivIllustIdTag) resultTags.Add(new Tag(pixivId, Settings.Default.SauceNao_TagNamespace_PixivIllustId) { Source = Enum.TagSource.SearchEngine });
					if (this.ShouldGetPixivMemberNameTag && !string.IsNullOrWhiteSpace(memberName)) resultTags.Add(new Tag(memberName, Settings.Default.SauceNao_TagNamespace_PixivMemberName) { Source = Enum.TagSource.SearchEngine });
					if (this.ShouldGetPixivMemberIdTag && !string.IsNullOrWhiteSpace(memberId)) resultTags.Add(new Tag(memberId, Settings.Default.SauceNao_TagNamespace_PixivMemberId) { Source = Enum.TagSource.SearchEngine });
				}

				// Danbooru / Gelbooru
				string danbooruId = (string)data.GetValue("danbooru_id");
				string gelbooruId = (string)data.GetValue("gelbooru_id");
				string creator = (string)data.GetValue("creator");
				string material = (string)data.GetValue("material");
				string characters = (string)data.GetValue("characters");

				if (!string.IsNullOrWhiteSpace(danbooruId)) resultTags.Add(new Tag(danbooruId, "danbooru-id") { Source = Enum.TagSource.SearchEngine });
				if (!string.IsNullOrWhiteSpace(gelbooruId)) resultTags.Add(new Tag(gelbooruId, "gelbooru-id") { Source = Enum.TagSource.SearchEngine });
				if (this.ShouldGetCreatorTag && !string.IsNullOrWhiteSpace(creator)) resultTags.Add(new Tag(creator, Settings.Default.SauceNao_TagNamespace_Creator) { Source = Enum.TagSource.SearchEngine });
				if (this.ShouldGetMaterialTag && !string.IsNullOrWhiteSpace(material)) resultTags.Add(new Tag(material, Settings.Default.SauceNao_TagNamespace_Material) { Source = Enum.TagSource.SearchEngine });
				if (this.ShouldGetCharacterTag && !string.IsNullOrWhiteSpace(characters)) resultTags.Add(new Tag(characters, Settings.Default.SauceNao_TagNamespace_Character) { Source = Enum.TagSource.SearchEngine });

				// Mangaupdates (overwrite the source from Danbooru/Gelbooru as there's normally only one or the other for a result)
				string muId = (string)data.GetValue("mu_id");
				string part = (string)data.GetValue("part");
				string type = (string)data.GetValue("type");

				if (!string.IsNullOrWhiteSpace(muId)) resultTags.Add(new Tag(muId, "manga-update-id") { Source = Enum.TagSource.SearchEngine });
				if (!string.IsNullOrWhiteSpace(part)) resultTags.Add(new Tag(part, "part") { Source = Enum.TagSource.SearchEngine });
				if (!string.IsNullOrWhiteSpace(type)) resultTags.Add(new Tag(type, "type") { Source = Enum.TagSource.SearchEngine });

				// Twitter
				string createdAt = (string)data.GetValue("created_at");
				string tweetId = (string)data.GetValue("tweet_id");
				string twitterUserId = (string)data.GetValue("twitter_user_id");
				string twitterUserHandle = (string)data.GetValue("twitter_user_handle");

				if (!string.IsNullOrWhiteSpace(createdAt)) resultTags.Add(new Tag(createdAt, "created-at") { Source = Enum.TagSource.SearchEngine });
				if (!string.IsNullOrWhiteSpace(tweetId)) resultTags.Add(new Tag(tweetId, "tweet-id") { Source = Enum.TagSource.SearchEngine });
				if (!string.IsNullOrWhiteSpace(twitterUserId)) resultTags.Add(new Tag(twitterUserId, "twitter-user-id") { Source = Enum.TagSource.SearchEngine });
				if (!string.IsNullOrWhiteSpace(twitterUserHandle)) resultTags.Add(new Tag(twitterUserHandle, "twitter-user-handle") { Source = Enum.TagSource.SearchEngine });

				// Nico Nico Seiga
				string seigaId = (string)data.GetValue("seiga_id");
				bool hasSeigaId = !string.IsNullOrWhiteSpace(seigaId);

				if (hasSeigaId) {
					resultTags.Add(new Tag(seigaId, "seiga-id") { Source = Enum.TagSource.SearchEngine });
					if (!string.IsNullOrWhiteSpace(memberName)) resultTags.Add(new Tag(memberName, "seiga-member-name") { Source = Enum.TagSource.SearchEngine });
					if (!string.IsNullOrWhiteSpace(memberId)) resultTags.Add(new Tag(memberId, "seiga-member-id") { Source = Enum.TagSource.SearchEngine });
				}

				foreach (string extUrl in extUrls) {
					this.AddMatch(extUrl, thumbnail, similarity, resultTags, string.IsNullOrWhiteSpace(source) ? source : null);
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
				if (!match.Tags.Contains(tag)) {
					match.Tags.Add(tag);
				}
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

		private bool ShouldGetTitleTag
		{
			get { return Settings.Default.SauceNao_TagNamespace_Title != null && !Settings.Default.SauceNao_TagNamespace_Title.StartsWith("-"); }
		}

		private bool ShouldGetCreatorTag
		{
			get { return Settings.Default.SauceNao_TagNamespace_Creator != null && !Settings.Default.SauceNao_TagNamespace_Creator.StartsWith("-"); }
		}

		private bool ShouldGetMaterialTag
		{
			get { return Settings.Default.SauceNao_TagNamespace_Material != null && !Settings.Default.SauceNao_TagNamespace_Material.StartsWith("-"); }
		}

		private bool ShouldGetCharacterTag
		{
			get { return Settings.Default.SauceNao_TagNamespace_Character != null && !Settings.Default.SauceNao_TagNamespace_Character.StartsWith("-"); }
		}

		private bool ShouldGetPixivIllustIdTag
		{
			get { return Settings.Default.SauceNao_TagNamespace_PixivIllustId != null && !Settings.Default.SauceNao_TagNamespace_PixivIllustId.StartsWith("-"); }
		}

		private bool ShouldGetPixivMemberIdTag
		{
			get { return Settings.Default.SauceNao_TagNamespace_PixivMemberId != null && !Settings.Default.SauceNao_TagNamespace_PixivMemberId.StartsWith("-"); }
		}

		private bool ShouldGetPixivMemberNameTag
		{
			get { return Settings.Default.SauceNao_TagNamespace_PixivMemberName != null && !Settings.Default.SauceNao_TagNamespace_PixivMemberName.StartsWith("-"); }
		}

		private bool ShouldGetTags
		{
			get
			{
				return this.ShouldGetTitleTag
					|| this.ShouldGetCreatorTag
					|| this.ShouldGetMaterialTag
					|| this.ShouldGetCharacterTag
					|| this.ShouldGetPixivIllustIdTag
					|| this.ShouldGetPixivMemberIdTag
					|| this.ShouldGetPixivMemberNameTag;
			}
		}

		#endregion Accessor
	}
}
