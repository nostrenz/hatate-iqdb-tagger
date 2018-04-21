namespace Hatate.Parser
{
	class Konachan : Page, IParser
	{
		/*
		============================================
		Protected
		============================================
		*/

		override protected bool Parse(Supremes.Nodes.Document doc)
		{
			Supremes.Nodes.Elements searchTags = doc.Select("#tag-sidebar li.tag-link");

			foreach (Supremes.Nodes.Element searchTag in searchTags) {
				string name = searchTag.Attr("data-name");

				if (string.IsNullOrEmpty(name)) {
					continue;
				}

				Tag tag = new Tag(name.Replace("_", " "));
				string type = searchTag.Attr("data-type");

				switch (type) {
					case "copyright": tag.Namespace = "series";    break;
					case "character": tag.Namespace = "character"; break;
					case "artist":    tag.Namespace = "creator";   break;
				}

				this.tags.Add(tag);
			}

			return true;
		}
	}
}
