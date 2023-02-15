namespace Hatate
{
	public class Image
	{
		/*
		Public
		*/

		public bool IsBetterThan(
			Image other,
			Enum.ImageFileFormat preferedFormat = Enum.ImageFileFormat.Unknown,
			Enum.ComparisonOperator widthOperator = Enum.ComparisonOperator.None,
			Enum.ComparisonOperator heightOperator = Enum.ComparisonOperator.None,
			Enum.ComparisonOperator sizeOperator = Enum.ComparisonOperator.None
		)
		{
			sbyte score = 0;

			if (preferedFormat != Enum.ImageFileFormat.Unknown && this.Format != Enum.ImageFileFormat.Unknown) {
				if (this.Format == preferedFormat && other.Format != preferedFormat) {
					score++;
				} else if (this.Format != preferedFormat && other.Format == preferedFormat) {
					score--;
				}
			}

			if (widthOperator != Enum.ComparisonOperator.None && this.Width > 0) {
				score += this.Compare(this.Width, other.Width, widthOperator);
			}

			if (heightOperator != Enum.ComparisonOperator.None && this.Height > 0) {
				score += this.Compare(this.Height, other.Height, heightOperator);
			}

			if (sizeOperator != Enum.ComparisonOperator.None && this.SizeInBytes > 0) {
				score += this.Compare(this.SizeInBytes, other.SizeInBytes, sizeOperator);
			}

			return score > 0;
		}

		/*
		Private
		*/

		private sbyte Compare(long value1, long value2, Enum.ComparisonOperator op)
		{
			if (op == Enum.ComparisonOperator.LessThan) {
				if (value1 < value2) return 1;
				if (value1 > value2) return -1;

				return 0;
			}

			if (op == Enum.ComparisonOperator.GreaterThan) {
				if (value1 > value2) return 1;
				if (value1 < value2) return -1;

				return 0;
			}

			if (op == Enum.ComparisonOperator.Equal) {
				if (value1 == value2) return 1;
				
				return -1;
			}

			if (op == Enum.ComparisonOperator.LessOrEqualThan) {
				if (value1 <= value2) return 1;
				
				return -1;
			}

			if (op == Enum.ComparisonOperator.GreaterOrEqualThan) {
				if (value1 >= value2) return 1;
				
				return -1;
			}

			return 0;
		}

		/*
		Accessor
		*/

		#region Accessor

		public int Width { get; set; }
		public int Height { get; set; }
		public long SizeInBytes { get; set; }
		public Enum.ImageFileFormat Format { get; set; }
		public string Hash { get; set; }

		public string FormatFromMimeType
		{
			set
			{
				switch (value) {
					case "image/jpg": this.Format = Enum.ImageFileFormat.JPEG; break;
					case "image/jpeg": this.Format = Enum.ImageFileFormat.JPEG; break;
					case "image/png": this.Format = Enum.ImageFileFormat.PNG; break;
					case "image/bmp": this.Format = Enum.ImageFileFormat.BMP; break;
					case "image/webp": this.Format = Enum.ImageFileFormat.WEBP; break;
					case "image/tiff": this.Format = Enum.ImageFileFormat.TIFF; break;
				}
			}
		}

		public string FormatFromFileExtension
		{
			set
			{
				switch (value) {
					case "jpg": this.Format = Enum.ImageFileFormat.JPEG; break;
					case "jpeg": this.Format = Enum.ImageFileFormat.JPEG; break;
					case "png": this.Format = Enum.ImageFileFormat.PNG; break;
					case "bmp": this.Format = Enum.ImageFileFormat.BMP; break;
					case "webp": this.Format = Enum.ImageFileFormat.WEBP; break;
					case "tiff": this.Format = Enum.ImageFileFormat.TIFF; break;
				}
			}
		}

		#endregion Accessor
	}
}
