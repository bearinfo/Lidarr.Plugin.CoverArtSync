namespace Lidarr.Plugin.CoverArtSync.Sources
{
    public interface IArtworkSource
    {
        string Name { get; }
        List<ArtworkImage> GetArtistImages(string musicBrainzId, string artistName);
        List<ArtworkImage> GetAlbumImages(string musicBrainzAlbumId, string albumTitle, string artistName);
    }
}
