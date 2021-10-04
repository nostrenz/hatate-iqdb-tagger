namespace Hatate.Parser
{
	class Yandere : Page, IParser
	{
		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			Supremes.Nodes.Element tagList = doc.Select("#tag-sidebar").First;

			if (tagList == null) {
				return false;
			}

			// Get tags
			this.AddTags(tagList, "copyright", "series");
			this.AddTags(tagList, "character", "character");
			this.AddTags(tagList, "artist", "creator");
			this.AddTags(tagList, "general");

			// Get rating
			if (Properties.Settings.Default.AddRating) {
				this.GetRating(doc, "#stats li");
			}

			// Get informations
			Supremes.Nodes.Elements statsLis = doc.Select("#stats li");
			Supremes.Nodes.Element highresLink = doc.Select("#highres").First;
			Supremes.Nodes.Element pngLink = doc.Select("#png").First;

			foreach (Supremes.Nodes.Element li in statsLis) {
				if (li.Text.StartsWith("Size: ")) {
					this.parseResolution(li.Text.Substring(li.Text.IndexOf(' ') + 1));
				}
			}

			if (pngLink != null) {
				this.GetFullImageUrlAndSizeFromLink(pngLink);
			} else if (highresLink != null) {
				this.GetFullImageUrlAndSizeFromLink(highresLink);
			}

			// Checks if the image is deleted
			Supremes.Nodes.Elements statusNotices= doc.Select(".status-notice");

			foreach (Supremes.Nodes.Element statusNotice in statusNotices) {
				if (statusNotice.Text.Contains("This post was deleted.")) {
					this.unavailable = true;

					break;
				}
			}

			return true;
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Add tags for a certain category.
		/// </summary>
		/// <param name="tagList"></param>
		/// <param name="type"></param>
		/// <param name="nameSpace"></param>
		private void AddTags(Supremes.Nodes.Element tagList, string category, string nameSpace=null)
		{
			Supremes.Nodes.Elements searchTags = tagList.Select("li.tag-type-" + category + " > a");

			foreach (Supremes.Nodes.Element searchTag in searchTags) {
				if (searchTag.Text == "?") {
					continue;
				}

				this.AddTag(searchTag.Text, nameSpace);
			}
		}

		/// <summary>
		/// Extract the full image URL and the image size from a link element.
		/// </summary>
		/// <param name="link"></param>
		private void GetFullImageUrlAndSizeFromLink(Supremes.Nodes.Element link)
		{
			this.full = link.Attr("href");

			string linkText = link.Text;
			linkText = linkText.Replace(" JPG", "");

			int start = linkText.LastIndexOf('(');
			int end = linkText.LastIndexOf(')');

			if (start > 0 && end > start) {
				this.size = this.KbOrMbToBytes(linkText.Substring(start + 1, end - start - 1));
			}
		}
	}
}
