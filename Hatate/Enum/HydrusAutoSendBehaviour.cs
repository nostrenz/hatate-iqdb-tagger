namespace Hatate.Enum
{
	/// <summary>
	/// Defines under which condition a result can be automatically sent to Hydrus.
	/// </summary>
	public enum HydrusAutoSendBehaviour : byte
	{
		Never,
		ImportLocal,
		ImportUrl,
		ImportUrlIfBetter,
		ImportUrlOrLocal
	}
}
