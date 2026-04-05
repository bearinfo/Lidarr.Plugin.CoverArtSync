using System.Text.Json;
using NLog;
using NzbDrone.Common.Http;

namespace Lidarr.Plugin.CoverArtSync.Sources
{
    /// <summary>
    /// TheAudioDB — artist/album art keyed by MusicBrainz ID.
    /// Free for basic use, no API key required (uses test key "2").
    /// </summary>
    public class TheAudioDbSource : IArtworkSource
    {
        private const string BaseUrl = "https://www.theaudiodb.com/api/v1/json/2";
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public string Name => "TheAudioDB";

        public TheAudioDbSource(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<ArtworkImage> GetArtistImages(string musicBrainzId, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                var request = new HttpRequest($"{BaseUrl}/artist-mb.php?i={musicBrainzId}");
                request.Headers.Accept = "application/json";
                request.SuppressHttpError = true;
                request.RateLimit = TimeSpan.FromSeconds(2);
                request.RateLimitKey = "TheAudioDB";

                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    _logger.Debug("TheAudioDB: No data for artist {0}", artistName);
                    return images;
                }

                var json = JsonDocument.Parse(response.Content);

                if (!json.RootElement.TryGetProperty("artists", out var artists) ||
                    artists.ValueKind == JsonValueKind.Null)
                {
                    return images;
                }

                foreach (var artist in artists.EnumerateArray())
                {
                    AddImageIfPresent(artist, "strArtistThumb", ArtworkType.Poster, images);
                    AddImageIfPresent(artist, "strArtistFanart", ArtworkType.Fanart, images);
                    AddImageIfPresent(artist, "strArtistFanart2", ArtworkType.Fanart, images);
                    AddImageIfPresent(artist, "strArtistFanart3", ArtworkType.Fanart, images);
                    AddImageIfPresent(artist, "strArtistFanart4", ArtworkType.Fanart, images);
                    AddImageIfPresent(artist, "strArtistLogo", ArtworkType.Logo, images);
                    AddImageIfPresent(artist, "strArtistBanner", ArtworkType.Banner, images);
                    AddImageIfPresent(artist, "strArtistClearart", ArtworkType.Clearlogo, images);
                    AddImageIfPresent(artist, "strArtistWideThumb", ArtworkType.Thumb, images);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "TheAudioDB: Failed to fetch images for artist {0}", artistName);
            }

            return images;
        }

        public List<ArtworkImage> GetAlbumImages(string musicBrainzAlbumId, string albumTitle, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                var request = new HttpRequest($"{BaseUrl}/album-mb.php?i={musicBrainzAlbumId}");
                request.Headers.Accept = "application/json";
                request.SuppressHttpError = true;
                request.RateLimit = TimeSpan.FromSeconds(2);
                request.RateLimitKey = "TheAudioDB";

                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    _logger.Debug("TheAudioDB: No data for album {0}", albumTitle);
                    return images;
                }

                var json = JsonDocument.Parse(response.Content);

                if (!json.RootElement.TryGetProperty("album", out var albums) ||
                    albums.ValueKind == JsonValueKind.Null)
                {
                    return images;
                }

                foreach (var album in albums.EnumerateArray())
                {
                    AddImageIfPresent(album, "strAlbumThumb", ArtworkType.Cover, images);
                    AddImageIfPresent(album, "strAlbumThumbBack", ArtworkType.Disc, images);
                    AddImageIfPresent(album, "strAlbumCDart", ArtworkType.Disc, images);
                    AddImageIfPresent(album, "strAlbumSpine", ArtworkType.Disc, images);
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "TheAudioDB: Failed to fetch images for album {0}", albumTitle);
            }

            return images;
        }

        private void AddImageIfPresent(JsonElement element, string property, ArtworkType type, List<ArtworkImage> images)
        {
            if (element.TryGetProperty(property, out var prop) &&
                prop.ValueKind == JsonValueKind.String &&
                prop.GetString() is string url &&
                !string.IsNullOrWhiteSpace(url))
            {
                images.Add(new ArtworkImage
                {
                    Url = url,
                    Type = type,
                    Source = Name
                });
            }
        }
    }
}
