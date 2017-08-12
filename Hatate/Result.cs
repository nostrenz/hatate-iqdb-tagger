﻿using System.Collections.Generic;

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
	}
}
