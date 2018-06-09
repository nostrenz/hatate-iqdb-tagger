namespace Hatate.Parser
{
	class Zerochan : Page, IParser
	{
		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			Supremes.Nodes.Elements tagRows = doc.Select("ul#tags li");

			if (tagRows == null) {
				return false;
			}

			foreach (Supremes.Nodes.Element tagRow in tagRows) {
				Supremes.Nodes.Elements link = tagRow.Select("a");

				if (link == null) {
					continue;
				}

				string value = link.Text.Replace(tagRow.Text, "").Trim();
				string nameSpace = tagRow.Text.Replace(value, "").Trim();

				switch (nameSpace) {
					case "Artiste":
						nameSpace = "creator";
					break;
					case "Studio":
						nameSpace = "series";
					break;
					case "Game":
						nameSpace = "series";
					break;
					case "Character":
						nameSpace = "character";
					break;
					case "Source":
						nameSpace = "series";
					break;
					default:
						nameSpace = null;
					break;
				}

				this.AddTag(value.ToLower(), nameSpace);
			}

			return true;
		}
	}
}
