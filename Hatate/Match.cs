using System.Collections.Immutable;
using IqdbApi.Enums;

/// <summary>
/// Mimics IqdbApi.Models.Match.
/// </summary>
namespace Hatate
{
	public class Match
	{
		/*
		============================================
		Public
		============================================
		*/

		/// <summary>
		/// Tryies to set the source from the URL.
		/// </summary>
		/// <returns>
		/// True if source was determined, false otherwise.
		/// </returns>
		public bool DetermineSourceFromUrl()
		{
			if (this.Url == null) {
				return false;
			}

			if (this.Url.Contains("danbooru.donmai.us")) {
				this.Source = Source.Danbooru;
			} else if (this.Url.Contains("gelbooru.com")) {
				this.Source = Source.Gelbooru;
			} else if (this.Url.Contains("anime-pictures.net")) {
				this.Source = Source.AnimePictures;
			} else if (this.Url.Contains("sankakucomplex.com")) {
				this.Source = Source.SankakuChannel;
			} else if (this.Url.Contains("konachan.com")) {
				this.Source = Source.Konachan;
			} else if (this.Url.Contains("yande.re")) {
				this.Source = Source.Yandere;
			} else if (this.Url.Contains("zerochan.net")) {
				this.Source = Source.Zerochan;
			} else if (this.Url.Contains("e-shuushuu.net")) {
				this.Source = Source.Eshuushuu;
			} else {
				return false;
			}

			return true;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public MatchType MatchType { get; internal set; }

		public string Url { get; internal set; }

		public string PreviewUrl { get; internal set; }

		public Rating Rating { get; internal set; }

		public byte? Score { get; internal set; }

		public ImmutableList<string> Tags { get; internal set; }

		public Source Source { get; internal set; }

		public IqdbApi.Models.Resolution Resolution { get; internal set; }

		public byte Similarity { get; internal set; }

		/// <summary>
		/// URL of the original image before being uploaded to a booru, generally from Pixiv or Twitter.
		/// </summary>
		public string SourceUrl { get; internal set; }
	}
}
