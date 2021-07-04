namespace Hatate.Parser
{
	class NicoNicoSeiga : Page, IParser
	{
		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			Supremes.Nodes.Element tagList = doc.Select("#illust_area div.lg_box_tag").First;

			if (tagList == null) {
				return false;
			}

			Supremes.Nodes.Elements tagItems = tagList.Select("a.tag");

			foreach (Supremes.Nodes.Element tagItem in tagItems) {
				if (tagItem != null) {
					this.AddTag(tagItem.Text);
				}
			}

			return true;
		}
	}
}
