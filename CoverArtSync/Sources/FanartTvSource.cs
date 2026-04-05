using System.Text.Json;
using NLog;
using NzbDrone.Common.Http;

namespace Lidarr.Plugin.CoverArtSync.Sources
{
    /// <summary>
    /// Fanart.tv — best source for artist-level artwork (posters, backgrounds, logos, banners).
    /// Uses MusicBrainz IDs. Requires a free API key from https://fanart.tv/get-an-api-key/
    /// </summary>
    public class FanartTvSource : IArtworkSource
    {
        private const string BaseUrl = "https://webservice.fanart.tv/v3/music";
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;
        private readonly string _apiKey;

        public string Name => "Fanart.tv";

        public FanartTvSource(IHttpClient httpClient, Logger logger, string apiKey)
        {
            _httpClient = httpClient;
            _logger = logger;
            _apiKey = apiKey;
        }

        public List<ArtworkImage> GetArtistImages(string musicBrainzId, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                var request = new HttpRequest($"{BaseUrl}/{musicBrainzId}?api_key={_apiKey}");
                request.Headers.Accept = "application/json";
                request.SuppressHttpError = true;
                request.RateLimit = TimeSpan.FromSeconds(1);
                request.RateLimitKey = "FanartTv";

                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    _logger.Debug("Fanart.tv: No data for artist {0} ({1})", artistName, musicBrainzId);
                    return images;
                }

                var json = JsonDocument.Parse(response.Content);

                ExtractImages(json.RootElement, "artistthumb", ArtworkType.Poster, images);
                ExtractImages(json.RootElement, "artistbackground", ArtworkType.Fanart, images);
                ExtractImages(json.RootElement, "hdmusiclogo", ArtworkType.Logo, images);
                ExtractImages(json.RootElement, "musiclogo", ArtworkType.Logo, images);
                ExtractImages(json.RootElement, "musicbanner", ArtworkType.Banner, images);
                ExtractImages(json.RootElement, "hdmusicart", ArtworkType.Clearlogo, images);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Fanart.tv: Failed to fetch images for artist {0}", artistName);
            }

            return images;
        }

        public List<ArtworkImage> GetAlbumImages(string musicBrainzAlbumId, string albumTitle, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                // Fanart.tv album art is accessed via the artist endpoint
                // and includes album-specific art keyed by release group ID
                // For album covers, we need the artist MBID, but since we may not have it here,
                // we rely on the albums section being present in artist responses.
                // This source is primarily for artist images.
                _logger.Trace("Fanart.tv: Album image lookup not directly supported for {0}", albumTitle);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Fanart.tv: Failed to fetch album images for {0}", albumTitle);
            }

            return images;
        }

        private void ExtractImages(JsonElement root, string property, ArtworkType type, List<ArtworkImage> images)
        {
            if (!root.TryGetProperty(property, out var array))
            {
                return;
            }

            foreach (var item in array.EnumerateArray())
            {
                if (item.TryGetProperty("url", out var urlProp) && urlProp.GetString() is string url)
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
}
