namespace Hatate.Parser
{
	class SankakuChannel : Page, IParser
	{
		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			Supremes.Nodes.Element tagSidebar = doc.Select("#tag-sidebar").First;
			
			// Get tags
			if (tagSidebar != null) {
				this.AddTags(tagSidebar, "copyright", "series");
				this.AddTags(tagSidebar, "character", "character");
				this.AddTags(tagSidebar, "artist", "creator");
				this.AddTags(tagSidebar, "medium", "meta");
				this.AddTags(tagSidebar, "general");
			}

			// Get details
			Supremes.Nodes.Element stats = doc.Select("#stats > ul").First;

			if (stats != null) {
				Supremes.Nodes.Elements listItems = stats.Select("li");

				foreach (Supremes.Nodes.Element li in listItems) {
					string content = li.Html;

					if (content == null) {
						continue;
					}

					content = content.Trim();

					if (content.StartsWith("Original:")) {
						Supremes.Nodes.Element full = li.Select("a").First;

						int space = full.Text.IndexOf(' ');

						if (full != null) {
							string size = full.Text.Substring(space + 1);
							string dimensions = full.Text.Substring(0, space);

							size = size.Substring(1, size.Length - 2);
							string[] parts = dimensions.Split('x');

							this.full = full.Attr("href");
							this.size = this.KbOrMbToBytes(size);

							if (parts.Length == 2) {
								int.TryParse(parts[0], out this.width);
								int.TryParse(parts[1], out this.height);
							}
						}
					} else if (content.StartsWith("Rating:")) {
						this.rating = content.Substring("Rating: ".Length);
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
		private void AddTags(Supremes.Nodes.Element tagSidebar, string type, string nameSpace=null)
		{
			Supremes.Nodes.Elements tagItems = tagSidebar.Select("li.tag-type-" + type + " > a");

			foreach (Supremes.Nodes.Element tagItem in tagItems) {
				this.AddTag(tagItem.Text, nameSpace);
			}
		}
	}
}
