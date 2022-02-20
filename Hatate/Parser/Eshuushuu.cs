namespace Hatate.Parser
{
	class Eshuushuu : Page, IParser
	{
		private const string URL = "https://e-shuushuu.net";

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

				if (rating != null && rating.Text != "N/A") {
					this.AddTag(rating.Text, "rating");
				}
			}

			// Get informations
			Supremes.Nodes.Element imageBlock = doc.Select("#content .image_block").First;

			if (imageBlock == null) {
				return true;
			}

			Supremes.Nodes.Element imageLink = imageBlock.Select(".thumb > a.thumb_image[href]").First;
			Supremes.Nodes.Elements metas = imageBlock.Select(".meta > dl").Select("> *");

			if (imageLink != null) {
				this.full = URL + imageLink.Attr("href");
			}

			for (byte i = 0; i < metas.Count; i += 2) {
				if (i+1 >= metas.Count) {
					break;
				}

				Supremes.Nodes.Element dt = metas[i];
				Supremes.Nodes.Element dd = metas[i+1];

				if (dt.Text == "File size:") {
					this.size = this.KbOrMbToBytes(dd.Text);
				} else if (dt.Text == "Dimensions:") {
					string dimensions = dd.Text;

					if (dimensions.Contains("(")) {
						dimensions = dimensions.Substring(0, dimensions.IndexOf(" "));
					}

					this.parseResolution(dimensions);
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
			if (tagList == null) {
				return;
			}

			Supremes.Nodes.Elements searchTags = tagList.Select("span > a");

			foreach (Supremes.Nodes.Element searchTag in searchTags) {
				this.AddTag(searchTag.Text, nameSpace);
			}
		}
	}
}
