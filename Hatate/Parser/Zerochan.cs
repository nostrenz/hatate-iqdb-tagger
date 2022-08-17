using System.Text.RegularExpressions;

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

			TagNamespaces.Zerochan tagNamespaces = new TagNamespaces.Zerochan();

			// Get tags
			foreach (Supremes.Nodes.Element tagRow in tagRows) {
				if (tagRow.Text.Length < 1) {
					continue;
				}

				string tag = tagRow.Text.Trim().ToLower();
				string nameSpace = tagRow.ClassName.ToLower();
				TagNamespace tagNamespace = null;

				switch (nameSpace) {
					case "artiste":
						tagNamespace = tagNamespaces.Artiste;
					break;
					case "studio":
						tagNamespace = tagNamespaces.Studio;
					break;
					case "game":
						tagNamespace = tagNamespaces.Game;
					break;
					case "mangaka":
						tagNamespace = tagNamespaces.Mangaka;
					break;
				}

				// Special case for the "source" namespace
				if (nameSpace == "source") {
					if (tag == "original") nameSpace = "series";
					else tagNamespace = tagNamespaces.Source;
				}

				if (tagNamespace != null && tagNamespace.Enabled) {
					nameSpace = tagNamespace.Namespace;
				}

				this.AddTag(tag, nameSpace);
			}

			// Get informations
			Supremes.Nodes.Element imageLink = doc.Select("#large > a.preview").First;
			Supremes.Nodes.Element imageElement = doc.Select("#large > img").First;
			Supremes.Nodes.Elements paragraphs = doc.Select("#large > p");

			if (imageLink != null) {
				this.full = imageLink.Attr("href");
			} else if (imageElement != null) {
				this.full = imageElement.Attr("src");
			}

			Regex resolutionnRegex = new Regex(@"\d+x\d+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

			foreach (Supremes.Nodes.Element paragraph in paragraphs) {
				MatchCollection resolutionMatches = resolutionnRegex.Matches(paragraph.OwnText);

				if (resolutionMatches.Count == 1) {
					GroupCollection groups = resolutionMatches[0].Groups;
					this.parseResolution(groups[0].Value);
				}

				Supremes.Nodes.Element span = paragraph.Select("> span").First;

				if (span != null && !string.IsNullOrWhiteSpace(span.OwnText)) {
					this.size = this.KbOrMbToBytes(span.OwnText);
				}
			}

			return true;
		}
	}
}
