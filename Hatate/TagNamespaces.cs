using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Options = Hatate.Properties.Settings;

namespace Hatate
{
	public class TagNamespaces
	{
		private const string TITLE = "Title";
		private const string CREATOR = "Creator";
		private const string MATERIAL = "Material";
		private const string CHARACTER = "Character";
		private const string PIXIV_ILLUST_ID = "Pixiv illust ID";
		private const string PIXIV_MEMBER_ID = "Pixiv member ID";
		private const string PIXIV_MEMBER_NAME = "Pixiv member name";
		private const string PART = "Part";
		private const string TYPE = "Type";
		private const string CREATED_AT = "Created at";
		private const string TWEET_ID = "Tweet ID";
		private const string TWITTER_USER_ID = "Twitter user ID";
		private const string TWITTER_USER_HANDLE = "Twitter user handle";
		private const string SEIGA_MEMBER_NAME = "Seiga member name";
		private const string SEIGA_MEMBER_ID = "Seiga member ID";
		private const string DANBOORU_ID = "Danbooru ID";
		private const string GELBOORU_ID = "Gelbooru ID";
		private const string MANGA_UPDATE_ID = "Manga update ID";
		private const string SEIGA_ID = "Seiga ID";

		private Dictionary<string, TagNamespace> tagNamespaces = new Dictionary<string, TagNamespace>() {};

		/*
		============================================
		Public
		============================================
		*/

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

		public void Add(string keyName, TagNamespace tagNamespace)
		{
			this.tagNamespaces.Add(keyName, tagNamespace);
		}

		public string Serialize()
		{
			return JsonConvert.SerializeObject(this.tagNamespaces);
		}

		private void CreateJson()
		{
			this.tagNamespaces.Add(TITLE, new TagNamespace("title"));
			this.tagNamespaces.Add(CREATOR, new TagNamespace("creator"));
			this.tagNamespaces.Add(MATERIAL, new TagNamespace("series"));
			this.tagNamespaces.Add(CHARACTER, new TagNamespace("character"));
			this.tagNamespaces.Add(PIXIV_ILLUST_ID, new TagNamespace("pixiv-illust-id"));
			this.tagNamespaces.Add(PIXIV_MEMBER_ID, new TagNamespace("pixiv-member-id"));
			this.tagNamespaces.Add(PIXIV_MEMBER_NAME, new TagNamespace("pixiv-member-name"));
			this.tagNamespaces.Add(PART, new TagNamespace("part"));
			this.tagNamespaces.Add(TYPE, new TagNamespace("type"));
			this.tagNamespaces.Add(CREATED_AT, new TagNamespace("created-at"));
			this.tagNamespaces.Add(TWEET_ID, new TagNamespace("tweet-id"));
			this.tagNamespaces.Add(TWITTER_USER_ID, new TagNamespace("twitter-user-id"));
			this.tagNamespaces.Add(TWITTER_USER_HANDLE, new TagNamespace("twitter-user-handle"));
			this.tagNamespaces.Add(SEIGA_MEMBER_NAME, new TagNamespace("seiga-member-name"));
			this.tagNamespaces.Add(SEIGA_MEMBER_ID, new TagNamespace("seiga-member-id"));
			this.tagNamespaces.Add(DANBOORU_ID, new TagNamespace("danbooru-id"));
			this.tagNamespaces.Add(GELBOORU_ID, new TagNamespace("gelbooru-id"));
			this.tagNamespaces.Add(MANGA_UPDATE_ID, new TagNamespace("manga-update-id"));
			this.tagNamespaces.Add(SEIGA_ID, new TagNamespace("seiga-id"));
		}

		private void LoadFromOptions()
		{
			this.tagNamespaces = JsonConvert.DeserializeObject<Dictionary<string, TagNamespace>>(Options.Default.TagNamespaces);
		}

		public Dictionary<string, TagNamespace> TagNamespacesList
		{
			get { return this.tagNamespaces; }
		}

		/*
		============================================
		Accessor
		============================================
		*/

		public TagNamespace Title
		{
			get { return this.tagNamespaces[TITLE]; }
		}

		public TagNamespace Creator
		{
			get { return this.tagNamespaces[CREATOR]; }
		}

		public TagNamespace Material
		{
			get { return this.tagNamespaces[MATERIAL]; }
		}

		public TagNamespace Character
		{
			get { return this.tagNamespaces[CHARACTER]; }
		}

		public TagNamespace PixivIllustId
		{
			get { return this.tagNamespaces[PIXIV_ILLUST_ID]; }
		}

		public TagNamespace PixivMemberId
		{
			get { return this.tagNamespaces[PIXIV_MEMBER_ID]; }
		}

		public TagNamespace PixivMemberName
		{
			get { return this.tagNamespaces[PIXIV_MEMBER_NAME]; }
		}

		public TagNamespace Part
		{
			get { return this.tagNamespaces[PART]; }
		}

		public TagNamespace Type
		{
			get { return this.tagNamespaces[TYPE]; }
		}

		public TagNamespace CreatedAt
		{
			get { return this.tagNamespaces[CREATED_AT]; }
		}

		public TagNamespace TweetId
		{
			get { return this.tagNamespaces[TWEET_ID]; }
		}

		public TagNamespace TwitterUserId
		{
			get { return this.tagNamespaces[TWITTER_USER_ID]; }
		}

		public TagNamespace TwitterUserHandle
		{
			get { return this.tagNamespaces[TWITTER_USER_HANDLE]; }
		}

		public TagNamespace SeigaMemberName
		{
			get { return this.tagNamespaces[SEIGA_MEMBER_NAME]; }
		}

		public TagNamespace SeigaMemberId
		{
			get { return this.tagNamespaces[SEIGA_MEMBER_ID]; }
		}

		public TagNamespace DanbooruId
		{
			get { return this.tagNamespaces[DANBOORU_ID]; }
		}

		public TagNamespace GelbooruId
		{
			get { return this.tagNamespaces[GELBOORU_ID]; }
		}

		public TagNamespace MangaUpdateId
		{
			get { return this.tagNamespaces[MANGA_UPDATE_ID]; }
		}

		public TagNamespace SeigaId
		{
			get { return this.tagNamespaces[SEIGA_ID]; }
		}
	}
}
