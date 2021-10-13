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

						int start = 0;
						int end = 0;

						if (full != null && full.Text != "»") {
							end = full.Text.LastIndexOf(' ');
							this.full = full.Attr("href");
							
							if (end > 0) {
								this.size = this.KbOrMbToBytes(full.Text.Substring(0, end));
							}
						}

						start = content.LastIndexOf('(');
						end = content.LastIndexOf(')');

						if (start > 0 && end > start) {
							this.parseResolution(content.Substring(start + 1, end - start - 1));
						}
					} else if (content.StartsWith("Rating:")) {
						this.rating = content.Substring("Rating:".Length);
					} else if (content.StartsWith("Source:")) {
						Supremes.Nodes.Element link = li.Select("a").First;

						if (link != null) {
							this.source = link.Attr("href");
						}
					}
				}
			}

			// Checks if the image is deleted
			Supremes.Nodes.Element postNoticeDeleted = doc.Select(".post-notice-deleted").First;
			Supremes.Nodes.Element postNoticeBanned = doc.Select(".post-notice-banned").First;

			if (postNoticeDeleted != null || postNoticeBanned != null) {
				this.unavailable = true;
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
