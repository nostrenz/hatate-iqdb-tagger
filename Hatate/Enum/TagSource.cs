namespace Hatate.Enum
{
	public enum TagSource : byte
	{
		Booru = 1, // Added from parsing a booru page 
		Auto = 2, // Auto-added tags (found, not found, tagged)
		User = 3, // Added manually by the user
		SauceNAO = 4 // Added from SauceNAO results
	}
}
