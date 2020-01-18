using Newtonsoft.Json.Linq;

namespace Hatate
{
	/// <summary>
	/// 
	/// </summary>
	public class HydrusMetadata
	{
		public HydrusMetadata(JToken token)
		{
			JObject inner = token.Value<JObject>();

			this.FileId = inner.GetValue("file_id").ToString();
			this.Hash = inner.GetValue("hash").ToString();
			this.Mime = inner.GetValue("mime").ToString();

			int width;
			int height;
			long size;

			int.TryParse(inner.GetValue("width").ToString(), out width);
			int.TryParse(inner.GetValue("height").ToString(), out height);
			long.TryParse(inner.GetValue("size").ToString(), out size);

			this.Width = width;
			this.Height = height;
			this.Size = size;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string FileId { get; set; }
		public string Hash { get; set; }
		public string Mime { get; set; }
		public int Width { get; set; }
		public int Height { get; set; }
		public long Size { get; set; }

		public bool IsImage
		{
			get { return this.Mime == "image/png" || this.Mime == "image/jpg"; }
		}

		#endregion Accessor
	}
}
