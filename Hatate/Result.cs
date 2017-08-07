using System.Collections.Generic;

namespace Hatate
{
	/// <summary>
	/// Represent the result of a searched image.
	/// </summary>
	class Result
	{
		public List<string> KnownTags { get; set; }

		public List<string> UnknownTags { get; set; }

		public string ThumbPath { get; set; }

		public string PreviewUrl { get; set; }

		public IqdbApi.Enums.Source Source { get; set; }
	}
}
