namespace Lidarr.Plugin.CoverArtSync.Sources
{
    public class ArtworkImage
    {
        public string Url { get; set; } = string.Empty;
        public ArtworkType Type { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public enum ArtworkType
    {
        Poster,
        Fanart,
        Banner,
        Logo,
        Cover,
        Disc,
        Clearlogo,
        Thumb
    }
}
