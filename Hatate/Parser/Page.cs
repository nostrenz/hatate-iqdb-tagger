using System;
using System.Collections.Generic;
using System.Globalization;

namespace Hatate.Parser
{
	// Base class for the booru parsers.
	abstract class Page
	{
		const string RATING = "Rating: ";

		private string url;
		protected List<Tag> tags = new List<Tag>();

		// Informations
		protected string full = null;
		protected long size = 0;
		protected int width = 0;
		protected int height = 0;
		protected string rating = null;

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
			this.tags.Add(new Tag(value, nameSpace) { Source = Hatate.Tag.SOURCE_BOORU });
		}

		/// <summary>
		/// Try to get the rating to add it as a tag.
		/// </summary>
		/// <param name="doc"></param>
		protected void GetRating(Supremes.Nodes.Element doc, string selector)
		{
			Supremes.Nodes.Element element = doc.Select(selector).First;

			if (element == null) {
				return;
			}

			element = element.NextElementSibling;

			while (element != null) {
				string text = element.Text;
				element = element.NextElementSibling;

				if (!text.StartsWith(RATING)) {
					continue;
				}

				string rating = text.Replace(RATING, "");

				if (!string.IsNullOrEmpty(rating)) {
					this.AddTag(rating.ToLower(), "rating");
				}
			}
		}

		/// <summary>
		/// Convert a string like "616 KB" or "7.25 MB" to bytes.
		/// </summary>
		/// <returns></returns>
		protected long KbOrMbToBytes(string size)
		{
			float value = 0;
			int multiplier = 0;

			if (size.EndsWith("KB")) {
				size = size.Substring(0, size.LastIndexOf("KB")).Trim();
				multiplier = 1000;
			} else if (size.EndsWith("MB")) {
				size = size.Substring(0, size.LastIndexOf("MB")).Trim();
				multiplier = 1000000;
			}

			float.TryParse(size, NumberStyles.AllowDecimalPoint, CultureInfo.CreateSpecificCulture("en-US"), out value);

			if (multiplier == 0 || value <= 0) {
				return 0;
			}

			return (long)(value * multiplier);
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

		public string Full
		{
			get { return this.full; }
		}

		public long Size
		{
			get { return this.size; }
		}

		public int Width
		{
			get { return this.width; }
		}

		public int Height
		{
			get { return this.height; }
		}

		public string Rating
		{
			get { return this.rating; }
		}

		protected string Url
		{
			get { return this.url; }
		}
	}
}
