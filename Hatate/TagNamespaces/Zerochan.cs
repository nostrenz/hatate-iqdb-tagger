using Options = Hatate.Properties.Settings;

namespace Hatate.TagNamespaces
{
	public class Zerochan : AbstractTagNamespaces
	{
		// Name of namespaces used by Zerochan
		private const string ARTISTE = "artiste";
		private const string STUDIO = "studio";
		private const string GAME = "game";
		private const string SOURCE = "source";
		private const string MANGAKA = "mangaka";
		private const string ARTBOOK = "artbook";
		private const string VTUBER = "vtuber";

		public Zerochan()
		{
			this.Init(Options.Default.ZerochanTagNamespaces);
		}

		/*
		============================================
		Public
		============================================
		*/

		/// <summary>
		/// Try to find a matching TagNamespace definition for a given namespace. 
		/// </summary>
		/// <param name="nameSpace">The name of a namespace from Zerochan</param>
		/// <returns></returns>
		public TagNamespace Find(string nameSpace)
		{	
			if (this.tagNamespaces.ContainsKey(nameSpace)) {
				return this.tagNamespaces[nameSpace];
			}

			return null;
		}

		/*
		============================================
		Protected
		============================================
		*/

		/// <summary>
		/// Create default namespace mapping.
		/// </summary>
		protected override void CreateDefault()
		{
			this.tagNamespaces.Add(ARTISTE, new TagNamespace("creator"));
			this.tagNamespaces.Add(STUDIO, new TagNamespace("creator"));
			this.tagNamespaces.Add(GAME, new TagNamespace("series"));
			this.tagNamespaces.Add(SOURCE, new TagNamespace("source"));
			this.tagNamespaces.Add(MANGAKA, new TagNamespace("creator"));
			this.tagNamespaces.Add(ARTBOOK, new TagNamespace("artbook"));
			this.tagNamespaces.Add(VTUBER, new TagNamespace("character"));
		}
	}
}
