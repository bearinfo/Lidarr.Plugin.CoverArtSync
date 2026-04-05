using System.Text.Json;
using NLog;
using NzbDrone.Common.Http;

namespace Lidarr.Plugin.CoverArtSync.Sources
{
    /// <summary>
    /// Cover Art Archive (coverartarchive.org) — album cover art via MusicBrainz release group IDs.
    /// Free, no API key needed. Rate limit: 1 req/sec.
    /// </summary>
    public class CoverArtArchiveSource : IArtworkSource
    {
        private const string BaseUrl = "https://coverartarchive.org";
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public string Name => "Cover Art Archive";

        public CoverArtArchiveSource(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public List<ArtworkImage> GetArtistImages(string musicBrainzId, string artistName)
        {
            // Cover Art Archive doesn't have artist images, only album/release art
            return new List<ArtworkImage>();
        }

        public List<ArtworkImage> GetAlbumImages(string musicBrainzAlbumId, string albumTitle, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                var request = new HttpRequest($"{BaseUrl}/release-group/{musicBrainzAlbumId}")
                {
                    RateLimit = TimeSpan.FromSeconds(1),
                    RateLimitKey = "CoverArtArchive"
                };
                request.Headers.Accept = "application/json";
                request.SuppressHttpError = true;

                var response = _httpClient.Get(request);

                if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.Debug("CoverArtArchive: No images found for release group {0}", musicBrainzAlbumId);
                    return images;
                }

                if (!response.HasHttpError)
                {
                    var json = JsonDocument.Parse(response.Content);
                    var imagesArray = json.RootElement.GetProperty("images");

                    foreach (var img in imagesArray.EnumerateArray())
                    {
                        var isFront = img.TryGetProperty("front", out var frontProp) && frontProp.GetBoolean();
                        var imageUrl = img.GetProperty("image").GetString();

                        if (imageUrl != null)
                        {
                            images.Add(new ArtworkImage
                            {
                                Url = imageUrl,
                                Type = isFront ? ArtworkType.Cover : ArtworkType.Disc,
                                Source = Name
                            });
                        }

                        // Also grab thumbnails structure for smaller sizes
                        if (img.TryGetProperty("thumbnails", out var thumbs))
                        {
                            if (thumbs.TryGetProperty("large", out var large) && large.GetString() is string largeUrl)
                            {
                                images.Add(new ArtworkImage
                                {
                                    Url = largeUrl,
                                    Type = isFront ? ArtworkType.Cover : ArtworkType.Disc,
                                    Width = 500,
                                    Source = Name
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "CoverArtArchive: Failed to fetch images for release group {0}", musicBrainzAlbumId);
            }

            return images;
        }
    }
}
