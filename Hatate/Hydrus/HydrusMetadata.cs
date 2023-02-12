using Newtonsoft.Json.Linq;

namespace Hatate
{
	/// <summary>
	/// 
	/// </summary>
	public class HydrusMetadata
	{
		private const string MIME_PNG = "image/png";
		private const string MIME_JPG = "image/jpg";
		private const string MIME_JPEG = "image/jpeg";
		private const string MIME_WEBP = "image/webp";
		private const string MIME_TIFF = "image/tiff";

		public HydrusMetadata(JToken token)
		{
			JObject inner = token.Value<JObject>();

			this.FileId = inner.GetValue("file_id").ToString();
			this.Hash = inner.GetValue("hash").ToString();
			this.Mime = inner.GetValue("mime").ToString();

			int width;
			int height;
			long sizeInBytes;

			int.TryParse(inner.GetValue("width").ToString(), out width);
			int.TryParse(inner.GetValue("height").ToString(), out height);
			long.TryParse(inner.GetValue("size").ToString(), out sizeInBytes);

			this.Width = width;
			this.Height = height;
			this.SizeInBytes = sizeInBytes;
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
		public long SizeInBytes { get; set; }

		public bool IsImage
		{
			get { return this.Mime == MIME_PNG || this.Mime == MIME_JPG || this.Mime == MIME_JPEG || this.Mime == MIME_WEBP || this.Mime == MIME_TIFF; }
		}

		public string Extension
		{
			get
			{
				switch (this.Mime) {
					case MIME_PNG:  return "png";
					case MIME_JPG:  return "jpg";
					case MIME_JPEG:  return "jpg";
					case MIME_WEBP:  return "webp";
					case MIME_TIFF: return "tiff";
					default: return "jpg";
				}
			}
		}

		#endregion Accessor
	}
}
