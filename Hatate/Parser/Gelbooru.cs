namespace Hatate.Parser
{
	class Gelbooru : Page, IParser
	{
		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			Supremes.Nodes.Element tagList = doc.Select("#tag-list").First;

			if (tagList == null) {
				return false;
			}

			// Get tags
			this.AddTags(tagList, "copyright", "series");
			this.AddTags(tagList, "character", "character");
			this.AddTags(tagList, "artist", "creator");
			this.AddTags(tagList, "general");
			this.AddTags(tagList, "metadata", "meta");

			// Get rating
			if (Properties.Settings.Default.AddRating) {
				this.GetRating(doc, "#tag-list li");
			}

			// Get informations
			Supremes.Nodes.Elements lis = tagList.Select("li");

			foreach (Supremes.Nodes.Element li in lis) {
				string content = li.Html;

				if (content == null) {
					continue;
				}

				content = content.Trim();

				if (content.Length < 1) {
					continue;
				}

				if (content.StartsWith("Size:")) {
					int end = content.IndexOf('<');

					if (end > 0) {
						content = content.Substring(6, end - 6);
						string[] parts = content.Trim().Split('x');

						if (parts.Length == 2) {
							int.TryParse(parts[0], out this.width);
							int.TryParse(parts[1], out this.height);
						}
					}
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
	}
}
