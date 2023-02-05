using System.Collections.Generic;

namespace Hatate.Parser
{
	interface IParser
	{
		bool FromUrl(string url);

		List<Tag> Tags { get; }
		string Full { get; }
		long Size { get; }
		int Width { get; }
		int Height { get; }
		string Rating { get; }
		bool Unavailable { get; }
		ushort Pages { get; }
	}
}
