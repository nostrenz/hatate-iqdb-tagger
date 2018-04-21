using System;
using System.Collections.Generic;

namespace Hatate.Parser
{
	class Danbooru
	{
		private string animeUrl;

		// Retrieved values
		private List<Tag> tags = new List<Tag>();

		/*
		============================================
		Public
		============================================
		*/

		public bool FromUrl(string url)
		{
			Supremes.Nodes.Document doc = null;

			// Search for the anime
			try {
				doc = Supremes.Dcsoup.Parse(new Uri(url), 5000);
			} catch {
				return false;
			}

			return this.Parse(doc);
		}

		public bool FromFile(string uri)
		{
			Supremes.Nodes.Document doc = null;

			// Search for the anime
			try {
				doc = Supremes.Dcsoup.ParseFile(uri, "utf-8");
			} catch {
				return false;
			}

			return this.Parse(doc);
		}

		/*
		============================================
		Private
		============================================
		*/

		private bool Parse(Supremes.Nodes.Document doc)
		{
			Supremes.Nodes.Element tagList = doc.Select("#tag-list").First;

			this.AddTags(tagList, "copyright", "series");
			this.AddTags(tagList, "character", "character");
			this.AddTags(tagList, "artist", "creator");
			this.AddTags(tagList, "general");
			this.AddTags(tagList, "meta", "meta");

			return true;
		}

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
				this.tags.Add(new Tag(searchTag.Text, nameSpace));
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public List<Tag> Tags
		{
			get { return this.tags; }
		}
	}
}
