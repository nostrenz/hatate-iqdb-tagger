namespace Hatate.Parser
{
	class Eshuushuu : Page, IParser
	{
		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			if (string.IsNullOrEmpty(this.Url)) {
				return false;
			}

			string url = this.Url;

			if (url.EndsWith("/")) {
				url = url.Substring(0, url.Length - 1);
			}

			int lastSlash = url.LastIndexOf('/');
			string imageId = url.Substring(lastSlash+1);

			Supremes.Nodes.Element tagList = doc.Select("#quicktag1_" + imageId).First;
			Supremes.Nodes.Element seriesList = doc.Select("#quicktag2_" + imageId).First;
			Supremes.Nodes.Element characterList = doc.Select("#quicktag4_" + imageId).First;
			Supremes.Nodes.Element artistList = doc.Select("#quicktag3_" + imageId).First;

			// Get tags
			this.AddTags(tagList);
			this.AddTags(seriesList, "series");
			this.AddTags(characterList, "character");
			this.AddTags(artistList, "creator");

			// Get rating
			if (Properties.Settings.Default.AddRating) {
				Supremes.Nodes.Element rating = doc.Select("#rating" + imageId).First;

				if (rating.Text != "N/A") {
					this.AddTag(rating.Text, "rating");
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
		private void AddTags(Supremes.Nodes.Element tagList, string nameSpace = null)
		{
			Supremes.Nodes.Elements searchTags = tagList.Select("span > a");

			foreach (Supremes.Nodes.Element searchTag in searchTags) {
				this.AddTag(searchTag.Text, nameSpace);
			}
		}
	}
}
