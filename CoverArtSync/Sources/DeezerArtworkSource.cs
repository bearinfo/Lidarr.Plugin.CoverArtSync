using System.Text.Json;
using System.Web;
using NLog;
using NzbDrone.Common.Http;

namespace Lidarr.Plugin.CoverArtSync.Sources
{
    /// <summary>
    /// Deezer — artist photos and album cover art.
    /// Free public API, no authentication needed.
    /// </summary>
    public class DeezerArtworkSource : IArtworkSource
    {
        private const string ApiBaseUrl = "https://api.deezer.com";
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public string Name => "Deezer";

        public DeezerArtworkSource(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<ArtworkImage> GetArtistImages(string musicBrainzId, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                var encoded = HttpUtility.UrlEncode(artistName);
                var request = new HttpRequest($"{ApiBaseUrl}/search/artist?q={encoded}&limit=5");
                request.Headers.Accept = "application/json";
                request.SuppressHttpError = true;
                request.RateLimit = TimeSpan.FromMilliseconds(200);
                request.RateLimitKey = "Deezer";

                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    _logger.Debug("Deezer: Search failed for artist {0}", artistName);
                    return images;
                }

                var json = JsonDocument.Parse(response.Content);

                if (!json.RootElement.TryGetProperty("data", out var data))
                {
                    return images;
                }

                foreach (var artist in data.EnumerateArray())
                {
                    var name = artist.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

                    if (name == null || !string.Equals(name, artistName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    // picture_xl is the highest resolution (1000x1000)
                    AddImageIfPresent(artist, "picture_xl", ArtworkType.Poster, images);
                    AddImageIfPresent(artist, "picture_big", ArtworkType.Poster, images);

                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Deezer: Failed to fetch images for artist {0}", artistName);
            }

            return images;
        }

        public List<ArtworkImage> GetAlbumImages(string musicBrainzAlbumId, string albumTitle, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                var query = HttpUtility.UrlEncode($"{artistName} {albumTitle}");
                var request = new HttpRequest($"{ApiBaseUrl}/search/album?q={query}&limit=5");
                request.Headers.Accept = "application/json";
                request.SuppressHttpError = true;
                request.RateLimit = TimeSpan.FromMilliseconds(200);
                request.RateLimitKey = "Deezer";

                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    _logger.Debug("Deezer: Search failed for album {0}", albumTitle);
                    return images;
                }

                var json = JsonDocument.Parse(response.Content);

                if (!json.RootElement.TryGetProperty("data", out var data))
                {
                    return images;
                }

                foreach (var album in data.EnumerateArray())
                {
                    var title = album.TryGetProperty("title", out var titleProp) ? titleProp.GetString() : null;

                    if (title == null || !string.Equals(title, albumTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    AddImageIfPresent(album, "cover_xl", ArtworkType.Cover, images);
                    AddImageIfPresent(album, "cover_big", ArtworkType.Cover, images);

                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Deezer: Failed to fetch images for album {0}", albumTitle);
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
