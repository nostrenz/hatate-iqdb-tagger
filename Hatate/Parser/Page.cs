using System;
using System.Collections.Generic;

namespace Hatate.Parser
{
	// Base class for the booru parsers.
	abstract class Page
	{
		const string RATING = "Rating: ";

		private string url;
		protected List<Tag> tags = new List<Tag>();

		/*
		============================================
		Public
		============================================
		*/

		public bool FromUrl(string url)
		{
			this.url = url;
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
		Protected
		============================================
		*/

		/// <summary>
		/// Destined to be overriden in the classes heriting from this one.
		/// </summary>
		/// <param name="doc"></param>
		/// <returns></returns>
		abstract protected bool Parse(Supremes.Nodes.Document doc);

		/// <summary>
		/// Add a new tag to the list.
		/// </summary>
		/// <param name="value"></param>
		/// <param name="nameSpace"></param>
		protected void AddTag(string value, string nameSpace=null)
		{
			this.tags.Add(new Tag(value, nameSpace));
		}

		/// <summary>
		/// Try to get the rating to add it as a tag.
		/// </summary>
		/// <param name="doc"></param>
		protected void GetRating(Supremes.Nodes.Element doc, string selector)
		{
			Supremes.Nodes.Element nextLi = doc.Select(selector).First.NextElementSibling;

			while (nextLi != null) {
				string text = nextLi.Text;
				nextLi = nextLi.NextElementSibling;

				if (!text.StartsWith(RATING)) {
					continue;
				}

				string rating = text.Replace(RATING, "");

				if (!string.IsNullOrEmpty(rating)) {
					this.AddTag(rating.ToLower(), "rating");
				}
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

		protected string Url
		{
			get { return this.url; }
		}
	}
}
