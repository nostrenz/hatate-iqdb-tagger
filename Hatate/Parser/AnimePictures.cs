namespace Hatate.Parser
{
	class AnimePictures : Page, IParser
	{
		private const string URL = "https://anime-pictures.net";
		private const string RESOLUTION = "Resolution: ";
		private const string SIZE = "Size: ";

		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			Supremes.Nodes.Element postTags = doc.Select("#post_tags > ul.tags").First;

			if (postTags == null) {
				return false;
			}

			// Get tags
			this.AddTags(postTags, "");
			this.AddTags(postTags, "green", "series");
			this.AddTags(postTags, "blue", "character");
			this.AddTags(postTags, "orange", "creator");

			// Get informations
			Supremes.Nodes.Element postContent = doc.Select("#content .post_content").First;
			Supremes.Nodes.Element fullImageLink = doc.Select("#big_preview_cont > a").First;

			if (postContent != null) {
				int resolutionStartIndex = postContent.Text.IndexOf(RESOLUTION) + RESOLUTION.Length;
				int resolutionEndIndex = postContent.Text.IndexOf(" ", resolutionStartIndex);
				string resolution = postContent.Text.Substring(resolutionStartIndex, resolutionEndIndex - resolutionStartIndex);

				int sizeStartIndex = postContent.Text.IndexOf(SIZE) + SIZE.Length;
				int sizeEndIndex = postContent.Text.IndexOf(" ", sizeStartIndex);
				string size = postContent.Text.Substring(sizeStartIndex, sizeEndIndex - sizeStartIndex);

				this.parseResolution(resolution);
				this.size = this.KbOrMbToBytes(size);
			}

			if (fullImageLink != null) {
				this.full = URL + fullImageLink.Attr("href");
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
		private void AddTags(Supremes.Nodes.Element tagList, string color, string nameSpace = null)
		{
			Supremes.Nodes.Elements searchTags = tagList.Select("li[class=\"" + color + "\"] > a");

			foreach (Supremes.Nodes.Element searchTag in searchTags) {
				this.AddTag(searchTag.Text, nameSpace);
			}
		}
	}
}
