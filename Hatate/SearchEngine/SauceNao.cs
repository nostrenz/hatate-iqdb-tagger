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

		// Tag namespaces
		private TagNamespace titleTagNamespace = App.tagNamespaces.Title;
		private TagNamespace pixivIllustIdNamespace = App.tagNamespaces.PixivIllustId;
		private TagNamespace pixivMemberNameNamespace = App.tagNamespaces.PixivMemberName;
		private TagNamespace pixivMemberIdNamespace = App.tagNamespaces.PixivMemberId;
		private TagNamespace creatorTagNamespace = App.tagNamespaces.Creator;
		private TagNamespace materialTagNamespace = App.tagNamespaces.Material;
		private TagNamespace characterTagNamespace = App.tagNamespaces.Character;
		private TagNamespace partTagNamespace = App.tagNamespaces.Part;
		private TagNamespace typeTagNamespace = App.tagNamespaces.Type;
		private TagNamespace createdAtTagNamespace = App.tagNamespaces.CreatedAt;
		private TagNamespace tweetIdTagNamespace = App.tagNamespaces.TweetId;
		private TagNamespace TwitterUserIdTagNamespace = App.tagNamespaces.TwitterUserId;
		private TagNamespace twitterUserHandleTagNamespace = App.tagNamespaces.TwitterUserHandle;
		private TagNamespace seigaMemberNameTagNamespace = App.tagNamespaces.SeigaMemberName;
		private TagNamespace seigaMemberIdTagNamespace = App.tagNamespaces.SeigaMemberId;
		private TagNamespace danbooruIdTagNamespace = App.tagNamespaces.DanbooruId;
		private TagNamespace gelbooruIdTagNamespace = App.tagNamespaces.GelbooruId;
		private TagNamespace mangaUpdateIdTagNamespace = App.tagNamespaces.MangaUpdateId;
		private TagNamespace seigaIdTagNamespace = App.tagNamespaces.SeigaId;

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
				Supremes.Nodes.Element resulttitle = resultcontent.Select(".resulttitle").First;
				Supremes.Nodes.Elements resultcontentcolumns = resultcontent.Select(".resultcontentcolumn");
				Supremes.Nodes.Elements originalSourceLinks = resultcontent.Select(".resultcontentcolumn a");

				// Title or creator tag
				if (resulttitle != null) {
					string titleText = resulttitle.Text;
					string nameSpace = this.titleTagNamespace.Namespace;

					if (titleText.StartsWith("Creator:") && this.creatorTagNamespace.Enabled) {
						titleText = titleText.Replace("Creator:", "");
						nameSpace = this.creatorTagNamespace.Namespace;

						resultTags.Add(new Tag(titleText.Trim(), nameSpace) { Source = Enum.TagSource.SearchEngine });
					} else if (this.titleTagNamespace.Enabled) {
						resultTags.Add(new Tag(titleText.Trim(), nameSpace) { Source = Enum.TagSource.SearchEngine });
					}
				}

				foreach (Supremes.Nodes.Element resultcontentcolumn in resultcontentcolumns) {
					string contentHtml = resultcontentcolumn.Html;

					// Get material tag
					if (this.materialTagNamespace.Enabled) {
						int materialPos = contentHtml.IndexOf("<strong>Material: </strong>");

						if (materialPos >= 0) {
							string materialHtml = contentHtml.Substring(materialPos);
							materialHtml = materialHtml.Replace("<strong>Material: </strong>", "");
							materialHtml = materialHtml.Replace("<br>", "");

							resultTags.Add(new Tag(materialHtml.Trim(), this.materialTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
						}
					}

					// Get character tags
					if (this.characterTagNamespace.Enabled) {
						int charactersPos = contentHtml.IndexOf("<strong>Characters: </strong>");

						if (charactersPos >= 0) {
							string charactersHtml = contentHtml.Replace("<strong>Characters: </strong>", "").Trim();
							charactersHtml = charactersHtml.Replace("<br>", "");
							string[] characterNames = charactersHtml.Split('\n');

							foreach (string characterName in characterNames) {
								if (!string.IsNullOrWhiteSpace(characterName)) {
									resultTags.Add(new Tag(characterName.Trim(), this.characterTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
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
							if (this.pixivIllustIdNamespace.Enabled) {
								resultTags.Add(new Tag(value, this.pixivIllustIdNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
							}
						} else if (key == "id") {
							if (this.pixivMemberIdNamespace.Enabled) {
								resultTags.Add(new Tag(value, this.pixivMemberIdNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
							}

							if (this.pixivMemberNameNamespace.Enabled) {
								resultTags.Add(new Tag(sourceLink.Text, this.pixivMemberNameNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
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

				if (this.titleTagNamespace.Enabled && !string.IsNullOrWhiteSpace(title)) resultTags.Add(new Tag(title, this.titleTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });

				// Pixiv
				string pixivId = (string)data.GetValue("pixiv_id");
				bool hasPixivId = !string.IsNullOrWhiteSpace(pixivId);

				if (hasPixivId) {
					if (this.pixivIllustIdNamespace.Enabled) resultTags.Add(new Tag(pixivId, this.pixivIllustIdNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
					if (this.pixivMemberNameNamespace.Enabled && !string.IsNullOrWhiteSpace(memberName)) resultTags.Add(new Tag(memberName, this.pixivMemberNameNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
					if (this.pixivMemberIdNamespace.Enabled && !string.IsNullOrWhiteSpace(memberId)) resultTags.Add(new Tag(memberId, this.pixivMemberIdNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
				}

				// Danbooru / Gelbooru
				string danbooruId = (string)data.GetValue("danbooru_id");
				string gelbooruId = (string)data.GetValue("gelbooru_id");
				string creator = (string)data.GetValue("creator");
				string material = (string)data.GetValue("material");
				string characters = (string)data.GetValue("characters");

				if (this.danbooruIdTagNamespace.Enabled && !string.IsNullOrWhiteSpace(danbooruId)) resultTags.Add(new Tag(danbooruId, this.danbooruIdTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
				if (this.gelbooruIdTagNamespace.Enabled && !string.IsNullOrWhiteSpace(gelbooruId)) resultTags.Add(new Tag(gelbooruId, this.gelbooruIdTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
				if (this.creatorTagNamespace.Enabled && !string.IsNullOrWhiteSpace(creator)) resultTags.Add(new Tag(creator, this.creatorTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
				if (this.materialTagNamespace.Enabled && !string.IsNullOrWhiteSpace(material)) resultTags.Add(new Tag(material, this.materialTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
				if (this.characterTagNamespace.Enabled && !string.IsNullOrWhiteSpace(characters)) resultTags.Add(new Tag(characters, this.characterTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });

				// Mangaupdates (overwrite the source from Danbooru/Gelbooru as there's normally only one or the other for a result)
				string muId = (string)data.GetValue("mu_id");
				string part = (string)data.GetValue("part");
				string type = (string)data.GetValue("type");

				if (this.mangaUpdateIdTagNamespace.Enabled && !string.IsNullOrWhiteSpace(muId)) resultTags.Add(new Tag(muId, this.mangaUpdateIdTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
				if (this.partTagNamespace.Enabled && !string.IsNullOrWhiteSpace(part)) resultTags.Add(new Tag(part, this.partTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
				if (this.typeTagNamespace.Enabled && !string.IsNullOrWhiteSpace(type)) resultTags.Add(new Tag(type, this.typeTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });

				// Twitter
				string createdAt = (string)data.GetValue("created_at");
				string tweetId = (string)data.GetValue("tweet_id");
				string twitterUserId = (string)data.GetValue("twitter_user_id");
				string twitterUserHandle = (string)data.GetValue("twitter_user_handle");

				if (this.createdAtTagNamespace.Enabled && !string.IsNullOrWhiteSpace(createdAt)) resultTags.Add(new Tag(createdAt, this.createdAtTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
				if (this.tweetIdTagNamespace.Enabled && !string.IsNullOrWhiteSpace(tweetId)) resultTags.Add(new Tag(tweetId, this.tweetIdTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
				if (this.TwitterUserIdTagNamespace.Enabled && !string.IsNullOrWhiteSpace(twitterUserId)) resultTags.Add(new Tag(twitterUserId, this.TwitterUserIdTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
				if (this.twitterUserHandleTagNamespace.Enabled && !string.IsNullOrWhiteSpace(twitterUserHandle)) resultTags.Add(new Tag(twitterUserHandle, this.twitterUserHandleTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });

				// Nico Nico Seiga
				string seigaId = (string)data.GetValue("seiga_id");
				bool hasSeigaId = !string.IsNullOrWhiteSpace(seigaId);

				if (hasSeigaId) {
					if (this.seigaIdTagNamespace.Enabled) resultTags.Add(new Tag(seigaId, this.seigaIdTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
					if (this.seigaMemberNameTagNamespace.Enabled && !string.IsNullOrWhiteSpace(memberName)) resultTags.Add(new Tag(memberName, this.seigaMemberNameTagNamespace.Namespace) { Source = Enum.TagSource.SearchEngine });
					if (this.seigaMemberIdTagNamespace.Enabled && !string.IsNullOrWhiteSpace(memberId)) resultTags.Add(new Tag(memberId, this.seigaMemberIdTagNamespace.Enabled) { Source = Enum.TagSource.SearchEngine });
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

		#endregion Accessor
	}
}
