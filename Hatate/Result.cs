using Hatate.Properties;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Cryptography;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using OptionsWindow = Hatate.Option;

namespace Hatate
{
	/// <summary>
	/// Represent the result of a searched image.
	/// </summary>
	public class Result : System.IEquatable<Result>
	{
		const byte FEW = 9;

		private Image local = new Image();
		private Image remote = new Image();
		private List<string> warnings = new List<string>();
		private int matchIndex = -1;
		private IqdbApi.Enums.Rating overrideRating = IqdbApi.Enums.Rating.Unrated;
		private bool unavailable = false;
		private ushort pages = 0;

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
				case "image/webp": this.local.Format = "webp"; break;
				case "image/tiff": this.local.Format = "tiff"; break;
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

		/// <summary>
		/// Remove all tags obtained from a certain source.
		/// </summary>
		public void ClearTagsOfSource(Enum.TagSource tagSource)
		{
			this.Tags.RemoveAll(tag => tag.Source == tagSource);
		}

		/// <summary>
		/// Reset this object.
		/// </summary>
		public void Reset()
		{
			this.Searched = false;
			this.Full = null;
			this.Rating = IqdbApi.Enums.Rating.Unrated;
			this.matchIndex = -1;

			this.Tags.Clear();
			this.Ignoreds.Clear();
			this.warnings.Clear();
			
			if (this.HasMatches) {
				this.Matches.Clear();
			}
		}

		/// <summary>
		/// Populate the Matches list from a list of IqdbApi.Models.Match.
		/// </summary>
		/// <param name="iqdbMatches"></param>
		public void UseIqdbApiMatches(ImmutableList<IqdbApi.Models.Match> iqdbMatches)
		{
			List<Match> matches = new List<Match>();

			foreach (IqdbApi.Models.Match iqdbMatch in iqdbMatches) {
				Match match = new Match();

				match.MatchType = iqdbMatch.MatchType;
				match.Url = iqdbMatch.Url;
				match.PreviewUrl = "http://iqdb.org" + iqdbMatch.PreviewUrl;
				match.Rating = iqdbMatch.Rating;
				match.Score = iqdbMatch.Score;
				match.Tags = iqdbMatch.Tags;
				match.Source = (Enum.Source)iqdbMatch.Source;
				match.Resolution = iqdbMatch.Resolution;
				match.Similarity = (float)iqdbMatch.Similarity;

				matches.Add(match);
			}

			this.Matches = ImmutableList.Create(matches.ToArray());
		}

		/// <summary>
		/// Removes all added warning messages.
		/// </summary>
		public void ClearWarnings()
		{
			this.warnings.Clear();
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Returns the highest similarity value across of matches.
		/// </summary>
		/// <returns></returns>
		private float GetHighestMatchSimilarity()
		{
			if (!this.HasMatches) {
				return 0;
			}

			float highestSimilarity = 0;

			foreach (Match match in this.Matches) {
				if (match.Similarity > highestSimilarity) {
					highestSimilarity = match.Similarity;
				}
			}

			return highestSimilarity;
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
		public string Full { get; set; }
		public string HydrusFileId { get; set; }
		public ImmutableList<Match> Matches { get; set; }

		public Match Match
		{
			get
			{
				if (!this.HasMatch) {
					return null;
				}

				return this.Matches[this.matchIndex];
			}
			set
			{
				this.matchIndex = this.Matches.IndexOf(value);
			}
		}

		public string Url
		{
			get { return this.Match != null ? this.Match.Url : null; }
		}

		public string PreviewUrl
		{
			get { return this.Match.PreviewUrl; }
		}

		public Enum.Source Source
		{
			get { return this.Match.Source; }
		}
		
		public IqdbApi.Enums.Rating Rating
		{
			get
			{
				if (this.overrideRating != IqdbApi.Enums.Rating.Unrated) {
					return this.overrideRating;
				}

				return this.Match.Rating;
			}
			set
			{
				this.overrideRating = value;
			}
		}

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
		public Image Remote
		{
			get { return this.remote; }
			set { this.remote = value; }
		}

		/// <summary>
		/// A non-null preview URL means that the image was found on IQDB.
		/// </summary>
		public bool Found
		{
			get { return this.HasMatches && this.HasMatch; }
		}

		public bool HasMatches
		{
			get { return this.Matches != null && this.Matches.Count > 0; }
		}

		public bool HasMatch
		{
			get { return this.matchIndex >= 0; }
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
				if (this.remote == null) {
					return false;
				}

				if (this.remote.Format != null && this.remote.Format.ToLower() == "png" && this.local.Format.ToLower() != "png") {
					return true;
				}

				if (this.remote.Width > this.local.Width || this.remote.Height > this.local.Height) {
					return true;
				}

				if (this.remote.Size > this.local.Size) {
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
				if (!this.Searched) {
					return this.ImagePath;
				}

				string parenthesisText = null;

				switch (Settings.Default.SearchedParenthesisValue) {
					case (byte)ParenthesisValue.NumberOfTags:
						parenthesisText = this.Tags.Count.ToString();
					break;
					case (byte)ParenthesisValue.NumberOfMatches:
						parenthesisText = this.HasMatches ? this.Matches.Count.ToString() : "0";
					break;
					case (byte)ParenthesisValue.MatchSource:
						parenthesisText = this.HasMatch ? this.Match.Source.ToString() : "unknown";
					break;
					case (byte)ParenthesisValue.MatchSimilarity:
						parenthesisText = (this.HasMatch ? ((int)this.Match.Similarity).ToString() : "0") + "%";
					break;
					case (byte)ParenthesisValue.HighestSimilarity:
						parenthesisText = ((int)this.GetHighestMatchSimilarity()).ToString() + "%";
					break;
				}

				if (parenthesisText == null) {
					return this.ImagePath;
				}

				return "(" + parenthesisText + ") " + this.ImagePath;
			}
		}

		/// <summary>
		/// An unavailable match means that the image is no longer available on the selected match's source page,
		/// because it was deleted, banned, or a lack of access rights prevents from obtaining the image's URL.
		/// Usually tags are still available but the image isn't displayed and there's no link to the full image.
		/// </summary>
		public bool Unavailable
		{
			get { return this.unavailable; }
			set { this.unavailable = value; }
		}

		/// <summary>
		/// Number of pages in an album.
		/// </summary>
		public ushort Pages
		{
			get { return this.pages; }
			set { this.pages = value; }
		}

		#endregion Accessor
	}
}
