using System.Collections.Immutable;
using IqdbApi.Enums;

/// <summary>
/// Mimics IqdbApi.Models.Match.
/// </summary>
namespace Hatate
{
	public class Match
	{
		private string url = null;
		private string previewUrl = null;
		private string sourceUrl = null;

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
		Private
		============================================
		*/

		/// <summary>
		/// Prepends the protocol to a URL if missing one.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public string FixUrl(string url)
		{
			if (url != null && url.StartsWith("//")) {
				return "https:" + url;
			}

			return url;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public MatchType MatchType { get; internal set; }

		public string Url
		{
			get { return this.FixUrl(this.url); }
			internal set { this.url = value; }
		}

		public string PreviewUrl
		{
			get { return this.FixUrl(this.previewUrl); }
			internal set { this.previewUrl = value; }
		}

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
		public string SourceUrl
		{
			get { return this.FixUrl(this.sourceUrl); }
			internal set { this.sourceUrl = value; }
		}

		/// <summary>
		/// Used in the GUI.
		/// </summary>
		public string ComboBoxLabel
		{
			get
			{
				string label = this.Source.ToString();

				if (this.Similarity > 0) {
					label += " " + this.Similarity + "%";
				}

				if (this.Resolution.Width > 0 && this.Resolution.Height > 0) {
					label += " " + this.Resolution.Width + "x" + this.Resolution.Height;
				}

				return label;
			}
		}
	}
}
