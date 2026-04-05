using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Web;
using NLog;
using NzbDrone.Common.Http;

namespace Lidarr.Plugin.CoverArtSync.Sources
{
    /// <summary>
    /// Spotify — high-resolution artist photos and album art.
    /// Requires Client ID and Client Secret from https://developer.spotify.com/dashboard
    /// Uses Client Credentials flow (no user login needed).
    /// </summary>
    public class SpotifyArtworkSource : IArtworkSource
    {
        private const string TokenUrl = "https://accounts.spotify.com/api/token";
        private const string ApiBaseUrl = "https://api.spotify.com/v1";
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private string? _accessToken;
        private DateTime _tokenExpiry = DateTime.MinValue;

        public string Name => "Spotify";

        public SpotifyArtworkSource(IHttpClient httpClient, Logger logger, string clientId, string clientSecret)
        {
            _httpClient = httpClient;
            _logger = logger;
            _clientId = clientId;
            _clientSecret = clientSecret;
        }

        public List<ArtworkImage> GetArtistImages(string musicBrainzId, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                if (!EnsureAccessToken())
                {
                    return images;
                }

                var encoded = HttpUtility.UrlEncode(artistName);
                var request = new HttpRequest($"{ApiBaseUrl}/search?q={encoded}&type=artist&limit=5");
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Accept = "application/json";
                request.SuppressHttpError = true;
                request.RateLimit = TimeSpan.FromMilliseconds(100);
                request.RateLimitKey = "Spotify";

                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    _logger.Debug("Spotify: Search failed for artist {0}", artistName);
                    return images;
                }

                var json = JsonDocument.Parse(response.Content);
                var artists = json.RootElement.GetProperty("artists").GetProperty("items");

                foreach (var artist in artists.EnumerateArray())
                {
                    var name = artist.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

                    if (name == null || !string.Equals(name, artistName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (artist.TryGetProperty("images", out var artistImages))
                    {
                        foreach (var img in artistImages.EnumerateArray())
                        {
                            if (img.TryGetProperty("url", out var urlProp) && urlProp.GetString() is string url)
                            {
                                var width = img.TryGetProperty("width", out var w) ? w.GetInt32() : (int?)null;
                                var height = img.TryGetProperty("height", out var h) ? h.GetInt32() : (int?)null;

                                images.Add(new ArtworkImage
                                {
                                    Url = url,
                                    Type = ArtworkType.Poster,
                                    Width = width,
                                    Height = height,
                                    Source = Name
                                });
                            }
                        }
                    }

                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Spotify: Failed to fetch images for artist {0}", artistName);
            }

            return images;
        }

        public List<ArtworkImage> GetAlbumImages(string musicBrainzAlbumId, string albumTitle, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                if (!EnsureAccessToken())
                {
                    return images;
                }

                var query = HttpUtility.UrlEncode($"album:{albumTitle} artist:{artistName}");
                var request = new HttpRequest($"{ApiBaseUrl}/search?q={query}&type=album&limit=5");
                request.Headers.Add("Authorization", $"Bearer {_accessToken}");
                request.Headers.Accept = "application/json";
                request.SuppressHttpError = true;
                request.RateLimit = TimeSpan.FromMilliseconds(100);
                request.RateLimitKey = "Spotify";

                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    _logger.Debug("Spotify: Search failed for album {0}", albumTitle);
                    return images;
                }

                var json = JsonDocument.Parse(response.Content);
                var albums = json.RootElement.GetProperty("albums").GetProperty("items");

                foreach (var album in albums.EnumerateArray())
                {
                    var name = album.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;

                    if (name == null || !string.Equals(name, albumTitle, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    if (album.TryGetProperty("images", out var albumImages))
                    {
                        foreach (var img in albumImages.EnumerateArray())
                        {
                            if (img.TryGetProperty("url", out var urlProp) && urlProp.GetString() is string url)
                            {
                                var width = img.TryGetProperty("width", out var w) ? w.GetInt32() : (int?)null;
                                var height = img.TryGetProperty("height", out var h) ? h.GetInt32() : (int?)null;

                                images.Add(new ArtworkImage
                                {
                                    Url = url,
                                    Type = ArtworkType.Cover,
                                    Width = width,
                                    Height = height,
                                    Source = Name
                                });
                            }
                        }
                    }

                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Spotify: Failed to fetch images for album {0}", albumTitle);
            }

            return images;
        }

        private bool EnsureAccessToken()
        {
            if (_accessToken != null && DateTime.UtcNow < _tokenExpiry)
            {
                return true;
            }

            try
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

                var request = new HttpRequest(TokenUrl);
                request.Method = System.Net.Http.HttpMethod.Post;
                request.Headers.Add("Authorization", $"Basic {credentials}");
                request.Headers.ContentType = "application/x-www-form-urlencoded";
                request.SetContent("grant_type=client_credentials");
                request.SuppressHttpError = true;

                var response = _httpClient.Post(request);

                if (response.HasHttpError)
                {
                    _logger.Error("Spotify: Failed to get access token");
                    return false;
                }

                var json = JsonDocument.Parse(response.Content);
                _accessToken = json.RootElement.GetProperty("access_token").GetString();
                var expiresIn = json.RootElement.GetProperty("expires_in").GetInt32();
                _tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);

                return true;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Spotify: Failed to authenticate");
                return false;
            }
        }
    }
}
