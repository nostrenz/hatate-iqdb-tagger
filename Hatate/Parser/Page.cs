using System;
using System.Collections.Generic;

namespace Hatate.Parser
{
	// Base class for the booru parsers.
	abstract class Page
	{
		protected List<Tag> tags = new List<Tag>();

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
		Protected
		============================================
		*/

		/// <summary>
		/// Destined to be overriden in the classes heriting from this one.
		/// </summary>
		/// <param name="doc"></param>
		/// <returns></returns>
		abstract protected bool Parse(Supremes.Nodes.Document doc);

		/*
		============================================
		Accessor
		============================================
		*/

		public List<Tag> Tags
		{
			get
			{
				return this.tags;
			}
		}
	}
}
