using System.Collections.Generic;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;

namespace Hatate
{
	/// <summary>
	/// Represent the result of a searched image.
	/// </summary>
	public class Result : System.IEquatable<Result>
	{
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
		public IqdbApi.Enums.Source Source { get; set; }
		public IqdbApi.Enums.Rating Rating { get; set; }

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

				if (!this.Found) {
					return Brushes.Red;
				}

				return (this.HasTags ? Brushes.LimeGreen : Brushes.Orange);
			}
		}

		#endregion Accessor
	}
}
