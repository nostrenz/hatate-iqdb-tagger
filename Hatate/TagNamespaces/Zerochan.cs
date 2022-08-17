using Options = Hatate.Properties.Settings;

namespace Hatate.TagNamespaces
{
	public class Zerochan : AbstractTagNamespaces
	{
		private const string ARTISTE = "artiste";
		private const string STUDIO = "studio";
		private const string GAME = "game";
		private const string SOURCE = "source";
		private const string MANGAKA = "mangaka";

		public Zerochan()
		{
			this.Init(Options.Default.ZerochanTagNamespaces);
		}

		/*
		============================================
		Protected
		============================================
		*/

		protected override void CreateDefault()
		{
			this.tagNamespaces.Add(ARTISTE, new TagNamespace("creator"));
			this.tagNamespaces.Add(STUDIO, new TagNamespace("creator"));
			this.tagNamespaces.Add(GAME, new TagNamespace("series"));
			this.tagNamespaces.Add(SOURCE, new TagNamespace("source"));
			this.tagNamespaces.Add(MANGAKA, new TagNamespace("creator"));
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public TagNamespace Artiste
		{
			get { return this.tagNamespaces[ARTISTE]; }
		}

		public TagNamespace Studio
		{
			get { return this.tagNamespaces[STUDIO]; }
		}

		public TagNamespace Game
		{
			get { return this.tagNamespaces[GAME]; }
		}

		public TagNamespace Source
		{
			get { return this.tagNamespaces[SOURCE]; }
		}

		public TagNamespace Mangaka
		{
			get { return this.tagNamespaces[MANGAKA]; }
		}
	}
}
