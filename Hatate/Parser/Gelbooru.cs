﻿namespace Hatate.Parser
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
			Supremes.Nodes.Element tagList = doc.Select("#tag-list > div").First;

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
		private void AddTags(Supremes.Nodes.Element tagList, string category, string nameSpace = null)
		{
			Supremes.Nodes.Elements searchTags = tagList.Select("li.tag-type-" + category + " > a");

			foreach (Supremes.Nodes.Element searchTag in searchTags) {
				if (searchTag.Text == "?") {
					continue;
				}

				this.tags.Add(new Tag(searchTag.Text, nameSpace));
			}
		}
	}
}