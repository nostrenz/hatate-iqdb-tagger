using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Options = Hatate.Properties.Settings;

namespace Hatate
{
	public class TagNamespaces
	{
		private List<TagNamespace> tagNamespaces = new List<TagNamespace>();

		public void Init()
		{
			// Empty setting, create default
			if (String.IsNullOrEmpty(Options.Default.TagNamespaces)) {
				this.CreateJson();
			} else {
				this.LoadFromOptions();
			}
		}

		public void Clear()
		{
			this.tagNamespaces.Clear();
		}

		public void Add(TagNamespace tagNamespace)
		{
			this.tagNamespaces.Add(tagNamespace);
		}

		public string Serialize()
		{
			return JsonConvert.SerializeObject(this.tagNamespaces);
		}

		private void CreateJson()
		{
			this.tagNamespaces.Add(new TagNamespace("title", "title"));
			this.tagNamespaces.Add(new TagNamespace("creator", "creator"));
			this.tagNamespaces.Add(new TagNamespace("material", "series"));
			this.tagNamespaces.Add(new TagNamespace("character", "character"));
			this.tagNamespaces.Add(new TagNamespace("pixivIllustId", "pixiv-illust-id"));
			this.tagNamespaces.Add(new TagNamespace("pixivMemberId", "pixiv-member-id"));
			this.tagNamespaces.Add(new TagNamespace("pixivIllustName", "pixiv-illust-name"));
			this.tagNamespaces.Add(new TagNamespace("part", "part"));
			this.tagNamespaces.Add(new TagNamespace("type", "type"));
			this.tagNamespaces.Add(new TagNamespace("createdAt", "created-at"));
			this.tagNamespaces.Add(new TagNamespace("tweetId", "tweet-id"));
			this.tagNamespaces.Add(new TagNamespace("twitterUserId", "twitter-user-id"));
			this.tagNamespaces.Add(new TagNamespace("twitterUserHandle", "twitter-user-handle"));
			this.tagNamespaces.Add(new TagNamespace("seigaMemberName", "seiga-member-name"));
			this.tagNamespaces.Add(new TagNamespace("seigaMemberId", "seiga-member-id"));
		}

		private void LoadFromOptions()
		{
			List<TagNamespace> tagNamespaces = JsonConvert.DeserializeObject<List<TagNamespace>>(Options.Default.TagNamespaces);

			foreach (TagNamespace tagNamespace in tagNamespaces) {
				this.tagNamespaces.Add(tagNamespace);
			}
		}

		public bool IsEnabled(string keyName)
		{
			foreach (TagNamespace tagNamespace in this.tagNamespaces) {
				if (tagNamespace.KeyName == keyName) {
					return tagNamespace.Enabled;
				}
			}

			return false;
		}

		public TagNamespace GetByKeyName(string keyName)
		{
			foreach (TagNamespace tagNamespace in this.tagNamespaces) {
				if (tagNamespace.KeyName == keyName) {
					return tagNamespace;
				}
			}

			return null;
		}

		public List<TagNamespace> TagNamespacesList
		{
			get { return this.tagNamespaces; }
		}
	}
}
