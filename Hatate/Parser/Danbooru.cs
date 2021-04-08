namespace Hatate.Parser
{
	class Danbooru : Page, IParser
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
			this.AddTags(tagList, "meta", "meta");

			// Get rating
			if (Properties.Settings.Default.AddRating) {
				this.GetRating(doc, "#post-information li");
			}

			// Get informations
			Supremes.Nodes.Element informations = doc.Select("#post-information").First;

			if (informations != null) {
				Supremes.Nodes.Elements listItems = informations.Select("ul li");

				foreach (Supremes.Nodes.Element li in listItems) {
					string content = li.Html;

					if (content == null) {
						continue;
					}

					content = content.Trim();

					if (content.StartsWith("Size:")) {
						Supremes.Nodes.Element full = li.Select("a").First;

						if (full != null) {
							this.full = full.Attr("href");
							this.size = this.KbOrMbToBytes(full.Text.Substring(0, full.Text.LastIndexOf(' ')));
						}

						int start = content.LastIndexOf('(');
						int end = content.LastIndexOf(')');

						if (start > 0 && end > 0) {
							string[] parts = content.Substring(start + 1, end - start - 1).Split('x');

							int.TryParse(parts[0], out this.width);
							int.TryParse(parts[1], out this.height);
						}
					} else if (content.StartsWith("Rating:")) {
						this.rating = content.Substring("Rating:".Length);
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
			Supremes.Nodes.Elements searchTags = tagList.Select("ul." + category + "-tag-list a.search-tag");

			foreach (Supremes.Nodes.Element searchTag in searchTags) {
				this.AddTag(searchTag.Text, nameSpace);
			}
		}
	}
}
