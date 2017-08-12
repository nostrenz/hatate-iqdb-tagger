﻿using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Hatate
{
	class Tag
	{
		public Tag(string value, string nameSpace=null)
		{
			this.Value = value;
			this.Namespace = nameSpace;
		}

		public string Value
		{
			get; set;
		}

		public string Namespace
		{
			get; set;
		}

		/// <summary>
		/// Return in format "namespace:value".
		/// </summary>
		public string Namespaced
		{
			get
			{
				if (string.IsNullOrEmpty(this.Namespace)) {
					return this.Value;
				}

				return this.Namespace + ":" + this.Value;
			}
		}

		/// <summary>
		/// Return in format "the_value".
		/// </summary>
		public string Underscored
		{
			get { return this.Value.Replace(" ", "_");  }
		}

		/// <summary>
		/// Return in format "the value".
		/// </summary>
		public string Whitespaced
		{
			get { return this.Value.Replace("_", " "); }
		}

		/// <summary>
		/// Foreground brush depending on the namespace.
		/// </summary>
		public Brush Foreground
		{
			get
			{
				switch (this.Namespace) {
					case "series": return Brushes.DeepPink;
					case "character": return Brushes.LimeGreen;
					case "creator": return Brushes.Brown;
					case "rating": return Brushes.LightSlateGray;
					default: return Brushes.CadetBlue;
				}
			}
		}
	}
}