using System.Collections.Generic;

namespace Hatate
{
	/// <summary>
	/// Represent the result of a searched image.
	/// </summary>
	class Result
	{
		public List<string> Tags { get; set; }

		public string ThumbPath { get; set; }

		public string PreviewUrl { get; set; }
	}
}
