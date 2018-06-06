namespace Hatate.Parser
{
	class TheAnimeGallery : Page, IParser
	{
		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			// There's no tags on this booru
			return false;
		}
	}
}
