using System.Collections.Generic;
using System.Security.Cryptography;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Hatate
{
	/// <summary>
	/// Represent the result of a searched image.
	/// </summary>
	public class Result : System.IEquatable<Result>
	{
		const byte FEW = 9;

		private Image local = new Image();
		private Image match = new Image();
		private List<string> warnings = new List<string>();

		public Result(string imagePath)
		{
			this.ImagePath = imagePath;

			this.Tags = new List<Tag>();
			this.Ignoreds = new List<Tag>();
		}

		/*
		============================================
		Public
		============================================
		*/

		/// <summary>
		/// Two Result objects are considered equals if thay have the same ImagePath.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(Result other)
		{
			return this.ImagePath == other.ImagePath;
		}

		/// <summary>
		/// Used when determining equality between two objects.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return this.ImagePath.GetHashCode();
		}

		public void SetHydrusMetadata(HydrusMetadata hydrusMetadata)
		{
			this.HydrusFileId = hydrusMetadata.FileId;

			this.local.Width = hydrusMetadata.Width;
			this.local.Height = hydrusMetadata.Height;
			this.local.Size = hydrusMetadata.Size;
			this.local.Hash = hydrusMetadata.Hash;

			switch (hydrusMetadata.Mime) {
				case "image/jpg": this.local.Format = "jpg"; break;
				case "image/jpeg": this.local.Format = "jpg"; break;
				case "image/png": this.local.Format = "png"; break;
				case "image/bmp": this.local.Format = "bmp"; break;
			}
		}

		public void AddWarning(string message)
		{
			if (!this.warnings.Contains(message)) {
				this.warnings.Add(message);
			}
		}

		/// <summary>
		/// Calculate the local image's MD5 hash.
		/// </summary>
		public void CalculateLocalHash()
		{
			using (var md5 = MD5.Create()) {
				using (var stream = System.IO.File.OpenRead(this.ImagePath)) {
					var hash = md5.ComputeHash(stream);

					this.local.Hash = System.BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
				}
			}
		}

		/*
		============================================
		Accessor
		============================================
		*/

		#region Accessor

		public string ImagePath { get; set; }
		public bool Searched { get; set; }
		public List<Tag> Tags { get; set; }
		public List<Tag> Ignoreds { get; set; }
		public string ThumbPath { get; set; }
		public string PreviewUrl { get; set; }
		public string Url { get; set; }
		public string Full { get; set; }
		public IqdbApi.Enums.Source Source { get; set; }
		public IqdbApi.Enums.Rating Rating { get; set; }
		public string HydrusFileId { get; set; }

		/// <summary>
		/// The local image file.
		/// </summary>
		public Image Local
		{
			get { return this.local; }
			set { this.Local = value; }
		}

		/// <summary>
		/// The found image from a booru.
		/// </summary>
		public Image Match
		{
			get { return this.match; }
			set { this.match = value; }
		}

		/// <summary>
		/// A non-null preview URL means that the image was found on IQDB.
		/// </summary>
		public bool Found
		{
			get { return this.PreviewUrl != null; }
		}

		/// <summary>
		/// Check if we have tags.
		/// </summary>
		public bool HasTags
		{
			get { return this.Tags.Count > 0; }
		}

		/// <summary>
		/// Check if we have known tags or ignoreds tags.
		/// </summary>
		public bool HasTagsOrIgnoreds
		{
			get { return this.HasTags || this.Ignoreds.Count > 0; }
		}

		public bool HasWarnings
		{
			get { return this.warnings.Count > 0; }
		}

		/// <summary>
		/// Text color in the Files listbox.
		/// </summary>
		public Brush Foreground
		{
			get
			{
				if (!this.Searched) {
					return (Brush)new System.Windows.Media.BrushConverter().ConvertFromString("#FFD2D2D2");
				}

				if (this.HasWarnings) {
					return Brushes.Orange;
				}

				if (!this.Found) {
					return Brushes.Red;
				}

				// Few tags were retrived, we should review this file
				if (this.Tags.Count <= FEW) {
					return Brushes.Yellow;
				}

				// Booru image seems better than the local one, we should review this file
				if (this.IsMatchBetterThanLocal) {
					return Brushes.Yellow;
				}

				return Brushes.LimeGreen;
			}
		}

		public string Tooltip
		{
			get
			{
				if (!this.Searched) {
					return null;
				}

				if (this.HasWarnings) {
					return "- " + string.Join("\n- ", this.warnings);
				}

				if (!this.Found) {
					return "Not found on IQDB";
				}

				string text = "Found on IQDB, ";

				// Few tags were retrived, we should review this file
				if (this.Tags.Count <= FEW) {
					return text + "but few tags were retrieved";
				}

				// Booru image seems better than the local one, we should review this file
				if (this.IsMatchBetterThanLocal) {
					return text + "booru image seems better";
				}

				return text + "local image seems better";
			}
		}

		/// <summary>
		/// Try to determinate if the distant file is better than the local one.
		/// </summary>
		/// <returns>
		/// True for the found file, False for the local file.
		/// </returns>
		public bool IsMatchBetterThanLocal
		{
			get
			{
				if (this.match == null) {
					return false;
				}

				if (this.Match.Format != null && this.Match.Format.ToLower() == "png" && this.Local.Format.ToLower() != "png") {
					return true;
				}

				if (this.Match.Width > this.Local.Width || this.Match.Height > this.Local.Height) {
					return true;
				}

				if (this.Match.Size > this.Local.Size) {
					return true;
				}

				return false;
			}
		}

		/// <summary>
		/// Text displayed in the listbox.
		/// </summary>
		public string Text
		{
			get
			{
				int count = this.Tags.Count;

				if (count < 1) {
					return this.ImagePath;
				}

				return "(" + count + ") " + this.ImagePath;
			}
		}

		#endregion Accessor
	}
}
