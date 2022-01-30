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

			// Get tags
			foreach (Supremes.Nodes.Element tagRow in tagRows) {
				Supremes.Nodes.Elements link = tagRow.Select("a");

				if (link == null) {
					continue;
				}

				if (tagRow.Text.Length < 1) {
					continue;
				}

				string value = link.Text.Replace(tagRow.Text, "").Trim();

				if (value.Length < 1) {
					continue;
				}

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
