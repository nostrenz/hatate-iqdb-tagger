using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Options = Hatate.Properties.Settings;

namespace Hatate
{
	public class Sources
	{
		private List<Source> sources = new List<Source>();

		public void Init()
		{
			// Empty sources, create default
			if (String.IsNullOrEmpty(Options.Default.Sources)) {
				this.CreateJson();
			} else {
				this.LoadFromOptions();
			}
		}

		public void Clear()
		{
			this.sources.Clear();
		}

		public void Add(Source source)
		{
			this.sources.Add(source);
		}

		public string Serialize()
		{
			return JsonConvert.SerializeObject(this.sources);
		}

		private void CreateJson()
		{
			Array sourceEnumValues = System.Enum.GetValues(typeof(Enum.Source));
			sbyte highestOrdering = 0;

			foreach (Enum.Source sourceEnumValue in sourceEnumValues) {
				sbyte ordering = 0;

				switch (sourceEnumValue) {
					case Enum.Source.Danbooru: ordering = Options.Default.Source_Danbooru; break;
					case Enum.Source.Konachan: ordering = Options.Default.Source_Konachan; break;
					case Enum.Source.Yandere: ordering = Options.Default.Source_Yandere; break;
					case Enum.Source.Gelbooru: ordering = Options.Default.Source_Gelbooru; break;
					case Enum.Source.SankakuChannel: ordering = Options.Default.Source_SankakuChannel; break;
					case Enum.Source.Eshuushuu: ordering = Options.Default.Source_Eshuushuu; break;
					case Enum.Source.TheAnimeGallery: ordering = Options.Default.Source_TheAnimeGallery; break;
					case Enum.Source.Zerochan: ordering = Options.Default.Source_Zerochan; break;
					case Enum.Source.AnimePictures: ordering = Options.Default.Source_AnimePictures; break;
					case Enum.Source.Pixiv: ordering = Options.Default.Source_Pixiv; break;
					case Enum.Source.Twitter: ordering = Options.Default.Source_Twitter; break;
					case Enum.Source.NicoNicoSeiga: ordering = Options.Default.Source_Seiga; break;
					case Enum.Source.DeviantArt: ordering = Options.Default.Source_DeviantArt; break;
					case Enum.Source.ArtStation: ordering = Options.Default.Source_ArtStation; break;
					case Enum.Source.Pawoo: ordering = Options.Default.Source_Pawoo; break;
					case Enum.Source.MangaDex: ordering = Options.Default.Source_MangaDex; break;
					case Enum.Source.Other: ordering = Options.Default.Source_Other; break;
					default: ordering = (sbyte)(highestOrdering + 1); break; // New sources will be added at the end
				}

				sbyte absoluteOrdering = Math.Abs(ordering);

				if (absoluteOrdering > highestOrdering) {
					highestOrdering = absoluteOrdering;
				}
				
				this.sources.Add(new Source(sourceEnumValue, ordering));
			}
		}

		private void LoadFromOptions()
		{
			List<Source> sources = JsonConvert.DeserializeObject<List<Source>>(Options.Default.Sources);
			List<Enum.Source> sourceEnumValues = new List<Enum.Source>();
			Array sourceEnumValuesArray = System.Enum.GetValues(typeof(Enum.Source));
			byte latestOrdering = 0;

			foreach (Enum.Source sourceValue in sourceEnumValuesArray) {
				sourceEnumValues.Add(sourceValue);
			}

			foreach (Source source in sources) {
				// This source does not exists, don't add it
				if (!sourceEnumValues.Contains(source.Value)) {
					continue;
				}

				latestOrdering = source.Ordering;

				sourceEnumValues.Remove(source.Value);
				this.sources.Add(source);
			}

			// Not all sources were added
			if (sourceEnumValues.Count > 0) {
				foreach (Enum.Source sourceEnumValue in sourceEnumValues) {
					latestOrdering++;

					this.sources.Add(new Source(sourceEnumValue, (sbyte)latestOrdering));
				}
			}
		}

		public bool IsEnabled(Enum.Source sourceEnumValue)
		{
			foreach (Source source in this.sources) {
				if (source.Value == sourceEnumValue) {
					return source.Enabled;
				}
			}

			return false;
		}

		public bool ShouldGetTags(Enum.Source sourceEnumValue)
		{
			foreach (Source source in this.sources) {
				if (source.Value == sourceEnumValue) {
					return source.GetTags;
				}
			}

			return false;
		}

		public Source GetByEnumValue(Enum.Source sourceEnumValue)
		{
			foreach (Source source in this.sources) {
				if (source.Value == sourceEnumValue) {
					return source;
				}
			}

			return null;
		}

		public List<Source> SourcesList
		{
			get { return this.sources; }
		}
	}
}
