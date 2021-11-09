using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hatate.Parser
{
	interface IParser
	{
		bool FromUrl(string url);
		bool FromFile(string uri);

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
