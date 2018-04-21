using System.Collections.Generic;

namespace Hatate
{
	/// <summary>
	/// Represent the result of a searched image.
	/// </summary>
	class Result
	{
		public Result()
		{
			this.KnownTags = new List<Tag>();
			this.UnknownTags = new List<Tag>();
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public bool Searched { get; set; }

		public List<Tag> KnownTags { get; set; }

		public List<Tag> UnknownTags { get; set; }

		public string ThumbPath { get; set; }

		public string PreviewUrl { get; set; }

		public string Url { get; set; }

		public IqdbApi.Enums.Source Source { get; set; }

		public IqdbApi.Enums.Rating Rating { get; set; }

		/// <summary>
		/// Result is greenlighted if found and has at least one tag.
		/// </summary>
		public bool Greenlight
		{
			get
			{
				// Not found
				if (!this.Found) {
					return false;
				}

				// Found, check the tags
				return this.HasKnownTags || this.UnknownTags.Count > 0;
			}
		}

		/// <summary>
		/// A non-null preview URL means that the image was found on IQDB.
		/// </summary>
		public bool Found
		{
			get { return this.PreviewUrl != null; }
		}

		/// <summary>
		/// Check if we have known tags.
		/// </summary>
		public bool HasKnownTags
		{
			get { return this.KnownTags.Count > 0; }
		}

		#endregion Accessor
	}
}
