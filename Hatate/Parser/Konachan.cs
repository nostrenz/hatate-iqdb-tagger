namespace Hatate.Parser
{
	class Konachan : Page, IParser
	{
		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			// Get tags
			this.GetTags(doc);

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
		/// Get tags from the document.
		/// </summary>
		/// <param name="doc"></param>
		private void GetTags(Supremes.Nodes.Document doc)
		{
			Supremes.Nodes.Elements searchTags = doc.Select("#tag-sidebar li.tag-link");

			foreach (Supremes.Nodes.Element searchTag in searchTags) {
				string name = searchTag.Attr("data-name");

				if (string.IsNullOrEmpty(name)) {
					continue;
				}

				Tag tag = new Tag(name.Replace("_", " ")) { Source = Hatate.Tag.SOURCE_BOORU };
				string type = searchTag.Attr("data-type");

				switch (type) {
					case "copyright": tag.Namespace = "series"; break;
					case "character": tag.Namespace = "character"; break;
					case "artist": tag.Namespace = "creator"; break;
					case "circle": tag.Namespace = "creator"; tag.Value += " (circle)"; break;
				}

				this.tags.Add(tag);
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
