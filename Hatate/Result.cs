using System.Collections.Generic;

namespace Hatate
{
	/// <summary>
	/// Represent the result of a searched image.
	/// </summary>
	class Result
	{
		public List<Tag> KnownTags { get; set; }

		public List<Tag> UnknownTags { get; set; }

		public string ThumbPath { get; set; }

		public string PreviewUrl { get; set; }

		public IqdbApi.Enums.Source Source { get; set; }

		public IqdbApi.Enums.Rating Rating { get; set; }

		/// <summary>
		/// Result is found if it has at least one tag.
		/// </summary>
		public bool Found
		{
			get
			{
				// We have known tags
				if (this.KnownTags != null && this.KnownTags.Count > 0) {
					return true;
				}

				// No known tags, return true if we have at least one unknown tag
				return (this.UnknownTags != null && this.UnknownTags.Count > 0);
			}
		}
	}
}
