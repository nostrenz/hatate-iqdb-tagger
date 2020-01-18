namespace Hatate
{
	public class Image
	{
		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public int Width { get; set; }
		public int Height { get; set; }
		public long Size { get; set; } // In bytes
		public string Format { get; set; } // Usually "jpg" or "png"
		public string Hash { get; set; }

		#endregion Accessor
	}
}
