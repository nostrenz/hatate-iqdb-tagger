namespace Hatate.Parser
{
	class DeviantArt : Page, IParser
	{
		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			Supremes.Nodes.Element tagList = doc.Select("div._2nerN > div._3UK_f").First;

			if (tagList == null) {
				return false;
			}

			Supremes.Nodes.Elements tagItems = tagList.Select("a.Q-jc6 > span._2ohCe");

			foreach (Supremes.Nodes.Element tagItem in tagItems) {
				if (tagItem != null) {
					this.AddTag(tagItem.Text);
				}
			}

			return true;
		}
	}
}
