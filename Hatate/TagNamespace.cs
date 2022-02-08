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
			this.KeyName = tagNamespaceItem.KeyName;
			this.Enabled = tagNamespaceItem.Enabled;
			this.Namespace = tagNamespaceItem.Namespace;
		}

		public TagNamespace(string keyName, string nameSpace)
		{
			this.KeyName = keyName;
			this.Namespace = nameSpace;
			this.Enabled = true;
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public string KeyName
		{
			get; set;
		}

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
