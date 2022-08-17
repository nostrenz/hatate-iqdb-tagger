using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Hatate.TagNamespaces
{
	abstract public class AbstractTagNamespaces
	{
		protected Dictionary<string, TagNamespace> tagNamespaces = new Dictionary<string, TagNamespace>() { };

		/*
		============================================
		Abstract
		============================================
		*/

		abstract protected void CreateDefault();

		/*
		============================================
		Public
		============================================
		*/

		public void Init(string json)
		{
			if (String.IsNullOrEmpty(json)) {
				// Empty setting, create default
				this.CreateDefault();
			} else {
				// Load from options
				this.tagNamespaces = JsonConvert.DeserializeObject<Dictionary<string, TagNamespace>>(json);
			}
		}

		public void Clear()
		{
			this.tagNamespaces.Clear();
		}

		public void Add(string keyName, TagNamespace tagNamespace)
		{
			this.tagNamespaces.Add(keyName, tagNamespace);
		}

		public string Serialize()
		{
			return JsonConvert.SerializeObject(this.tagNamespaces);
		}

		/*
		============================================
		Private
		============================================
		*/

		public Dictionary<string, TagNamespace> TagNamespacesList
		{
			get { return this.tagNamespaces; }
		}
	}
}
