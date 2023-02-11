using System.Collections.Generic;
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
		private List<Tag> tags = new List<Tag>();

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
			this.Source = Enum.Source.Other;

			string url = this.Url;

			if (url == null) {
				return;
			}

			if (url.Contains("danbooru.donmai.us")) {
				this.Source = Enum.Source.Danbooru;
			} else if (url.Contains("gelbooru.com")) {
				this.Source = Enum.Source.Gelbooru;
			} else if (url.Contains("anime-pictures.net")) {
				this.Source = Enum.Source.AnimePictures;
			} else if (url.Contains("sankakucomplex.com")) {
				this.Source = Enum.Source.SankakuChannel;
			} else if (url.Contains("konachan.com")) {
				this.Source = Enum.Source.Konachan;
			} else if (url.Contains("yande.re")) {
				this.Source = Enum.Source.Yandere;
			} else if (url.Contains("zerochan.net")) {
				this.Source = Enum.Source.Zerochan;
			} else if (url.Contains("e-shuushuu.net")) {
				this.Source = Enum.Source.Eshuushuu;
			} else if (url.Contains("pixiv.net")) {
				this.Source = Enum.Source.Pixiv;
			} else if (url.Contains("twitter.com")) {
				this.Source = Enum.Source.Twitter;
			} else if (url.Contains("seiga.nicovideo.jp")) {
				this.Source = Enum.Source.NicoNicoSeiga;
			} else if (url.Contains("deviantart.com")) {
				this.Source = Enum.Source.DeviantArt;
			} else if (url.Contains("artstation.com")) {
				this.Source = Enum.Source.ArtStation;
			} else if (url.Contains("pawoo.net")) {
				this.Source = Enum.Source.Pawoo;
			} else if (url.Contains("mangadex.org")) {
				this.Source = Enum.Source.MangaDex;
			} else if (url.Contains("e621.net")) {
				this.Source = Enum.Source.e621;
			}
		}

		/*
		============================================
		Private
		============================================
		*/

		/// <summary>
		/// Modify a URL to be usable by Hatate and Hydrus.
		/// </summary>
		/// <param name="url"></param>
		/// <returns></returns>
		public string FixUrl(string url)
		{
			if (url == null) {
				return url;
			}

			// Prepend protocol when missing
			if (url.StartsWith("//")) {
				url = "https:" + url;
			}

			// Fix danbooru URL from a SauceNAO search
			if (this.Source == Enum.Source.Danbooru) {
				url = url.Replace("/post/show/", "/posts/");
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

		public List<Tag> Tags
		{
			get { return this.tags; }
		}

		public Enum.Source Source { get; internal set; }

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
				string url = this.Url;

				if (this.Source == Enum.Source.Other && url != null) {
					// We don't recognize the source so we'll display the URL's domain instead
					try {
						int start = url.IndexOf("://") + 3;
						int end = url.IndexOf('/', start);

						label = url.Substring(start, end - start);
					} catch (System.ArgumentOutOfRangeException) { }
				}

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
