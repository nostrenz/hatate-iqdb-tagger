/// <summary>
/// Mimics IqdbApi.Enums.Source.
/// </summary>
namespace Hatate
{
	public enum Source : byte
    {
        Danbooru,
        Konachan,
        Yandere,
        Gelbooru,
        SankakuChannel,
        Eshuushuu,
        TheAnimeGallery,
        Zerochan,
        AnimePictures,

        // New sources not in IqdbApi.Enums.Source

        Other, // To be used if we can't determine the source
        Pixiv,
        Seiga, // https://seiga.nicovideo.jp
        Twitter,
        DeviantArt,
        ArtStation,
        Pawoo,
        MangaDex
    }
}
