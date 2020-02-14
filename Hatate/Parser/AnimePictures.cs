namespace Hatate.Parser
{
	class AnimePictures : Page, IParser
	{
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
