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
		public void DetermineSourceFromUrl()
		{
			this.Source = Source.Other;

			if (this.Url == null) {
				return;
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
			} else if (this.Url.Contains("pixiv.net")) {
				this.Source = Source.Pixiv;
			} else if (this.Url.Contains("twitter.com")) {
				this.Source = Source.Twitter;
			} else if (this.Url.Contains("seiga.nicovideo.jp")) {
				this.Source = Source.Seiga;
			}
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

		public float Similarity { get; internal set; }

		/// <summary>
		/// If this.Url is set with the URL of a booru (Danbooru, ect) then this can be set with the URL to the original source
		/// (like Pixiv, Twitter, etc). This allows to retrieve tags from the booru but then having the possibility to send the
		/// original source URL to Hydrus with those tags instead of the booru URL.
		/// </summary>
		public string SourceUrl { get; internal set; }
	}
}
