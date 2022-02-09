using Hatate.View.Control;

namespace Hatate
{
	public class TagNamespace
	{
		public TagNamespace()
		{
		}

		public TagNamespace(TagNamespaceItem tagNamespaceItem)
		{
			this.Enabled = tagNamespaceItem.Enabled;
			this.Namespace = tagNamespaceItem.Namespace;
		}

		public TagNamespace(string nameSpace)
		{
			this.Namespace = nameSpace;
			this.Enabled = true;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public bool Enabled
		{
			get; set;
		}

		public string Namespace
		{
			get; set;
		}
	}
}
