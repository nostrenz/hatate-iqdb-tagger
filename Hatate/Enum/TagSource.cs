namespace Hatate.Enum
{
	public enum TagSource : byte
	{
		Booru = 1, // Added from parsing a booru page 
		Hatate = 2, // Added by Hatate ("found", "not found", "tagged", ...)
		User = 3, // Added manually by the user
		SearchEngine = 4 // Added by parsing the search engine's results page
	}
}
