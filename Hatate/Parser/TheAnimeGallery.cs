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
			Supremes.Nodes.Elements tagRows = doc.Select("#tagbox > ul.taglist > li > a");

			if (tagRows == null) {
				return false;
			}

			foreach (Supremes.Nodes.Element tagRow in tagRows) {
				string nameSpace = tagRow.Attr("class");

				// Series is the only know namespace for this booru right now
				if (nameSpace != "series") {
					nameSpace = null;
				}

				this.AddTag(tagRow.Text, nameSpace);
			}

			return true;
		}
	}
}
