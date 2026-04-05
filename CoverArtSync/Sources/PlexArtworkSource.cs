using System.Text.Json;
using System.Web;
using NLog;
using NzbDrone.Common.Http;

namespace Lidarr.Plugin.CoverArtSync.Sources
{
    /// <summary>
    /// Plex — fetch artwork from a local Plex Media Server instance.
    /// Requires Plex URL and authentication token.
    /// </summary>
    public class PlexArtworkSource : IArtworkSource
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;
        private readonly string _plexUrl;
        private readonly string _plexToken;
        private readonly string _librarySectionId;

        public string Name => "Plex";

        public PlexArtworkSource(IHttpClient httpClient, Logger logger, string plexUrl, string plexToken, string librarySectionId)
        {
            _httpClient = httpClient;
            _logger = logger;
            _plexUrl = plexUrl.TrimEnd('/');
            _plexToken = plexToken;
            _librarySectionId = librarySectionId;
        }

        public List<ArtworkImage> GetArtistImages(string musicBrainzId, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                var sections = GetMusicSections();

                foreach (var sectionId in sections)
                {
                    var artistKey = SearchForArtist(sectionId, artistName);
                    if (artistKey == null)
                    {
                        continue;
                    }

                    var metadataResult = GetMetadata(artistKey);
                    if (metadataResult == null)
                    {
                        continue;
                    }

                    var metadata = metadataResult.Value;

                    if (metadata.TryGetProperty("thumb", out var thumb) &&
                        thumb.GetString() is string thumbUrl)
                    {
                        images.Add(new ArtworkImage
                        {
                            Url = BuildPlexImageUrl(thumbUrl),
                            Type = ArtworkType.Poster,
                            Source = Name
                        });
                    }

                    if (metadata.TryGetProperty("art", out var art) &&
                        art.GetString() is string artUrl)
                    {
                        images.Add(new ArtworkImage
                        {
                            Url = BuildPlexImageUrl(artUrl),
                            Type = ArtworkType.Fanart,
                            Source = Name
                        });
                    }

                    if (images.Count > 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Plex: Failed to fetch images for artist {0}", artistName);
            }

            return images;
        }

        public List<ArtworkImage> GetAlbumImages(string musicBrainzAlbumId, string albumTitle, string artistName)
        {
            var images = new List<ArtworkImage>();

            try
            {
                var sections = GetMusicSections();

                foreach (var sectionId in sections)
                {
                    var albumKey = SearchForAlbum(sectionId, albumTitle, artistName);
                    if (albumKey == null)
                    {
                        continue;
                    }

                    var metadataResult = GetMetadata(albumKey);
                    if (metadataResult == null)
                    {
                        continue;
                    }

                    var metadata = metadataResult.Value;

                    if (metadata.TryGetProperty("thumb", out var thumb) &&
                        thumb.GetString() is string thumbUrl)
                    {
                        images.Add(new ArtworkImage
                        {
                            Url = BuildPlexImageUrl(thumbUrl),
                            Type = ArtworkType.Cover,
                            Source = Name
                        });
                    }

                    if (images.Count > 0)
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Plex: Failed to fetch images for album {0}", albumTitle);
            }

            return images;
        }

        private List<string> GetMusicSections()
        {
            if (!string.IsNullOrWhiteSpace(_librarySectionId))
            {
                return new List<string> { _librarySectionId };
            }

            var sections = new List<string>();

            try
            {
                var request = BuildPlexRequest("/library/sections");
                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    return sections;
                }

                var json = JsonDocument.Parse(response.Content);
                var container = json.RootElement.GetProperty("MediaContainer");

                if (container.TryGetProperty("Directory", out var directories))
                {
                    foreach (var dir in directories.EnumerateArray())
                    {
                        if (dir.TryGetProperty("type", out var type) &&
                            type.GetString() == "artist")
                        {
                            if (dir.TryGetProperty("key", out var key))
                            {
                                sections.Add(key.GetString()!);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Plex: Failed to get library sections");
            }

            return sections;
        }

        private string? SearchForArtist(string sectionId, string artistName)
        {
            try
            {
                var encoded = HttpUtility.UrlEncode(artistName);
                var request = BuildPlexRequest($"/library/sections/{sectionId}/all?type=8&title={encoded}");
                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    return null;
                }

                var json = JsonDocument.Parse(response.Content);
                var container = json.RootElement.GetProperty("MediaContainer");

                if (container.TryGetProperty("Metadata", out var metadata))
                {
                    foreach (var item in metadata.EnumerateArray())
                    {
                        if (item.TryGetProperty("title", out var title) &&
                            string.Equals(title.GetString(), artistName, StringComparison.OrdinalIgnoreCase))
                        {
                            return item.TryGetProperty("key", out var key) ? key.GetString() : null;
                        }
                    }

                    // Fallback: return first result
                    var firstItem = metadata.EnumerateArray().GetEnumerator();
                    if (firstItem.MoveNext() && firstItem.Current.TryGetProperty("key", out var firstKey))
                    {
                        return firstKey.GetString();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Plex: Failed to search for artist {0}", artistName);
            }

            return null;
        }

        private string? SearchForAlbum(string sectionId, string albumTitle, string artistName)
        {
            try
            {
                var encoded = HttpUtility.UrlEncode(albumTitle);
                var request = BuildPlexRequest($"/library/sections/{sectionId}/all?type=9&title={encoded}");
                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    return null;
                }

                var json = JsonDocument.Parse(response.Content);
                var container = json.RootElement.GetProperty("MediaContainer");

                if (container.TryGetProperty("Metadata", out var metadata))
                {
                    foreach (var item in metadata.EnumerateArray())
                    {
                        var titleMatch = item.TryGetProperty("title", out var title) &&
                                         string.Equals(title.GetString(), albumTitle, StringComparison.OrdinalIgnoreCase);

                        var artistMatch = !item.TryGetProperty("parentTitle", out var parent) ||
                                          string.Equals(parent.GetString(), artistName, StringComparison.OrdinalIgnoreCase);

                        if (titleMatch && artistMatch)
                        {
                            return item.TryGetProperty("key", out var key) ? key.GetString() : null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Plex: Failed to search for album {0}", albumTitle);
            }

            return null;
        }

        private JsonElement? GetMetadata(string key)
        {
            try
            {
                var request = BuildPlexRequest(key);
                var response = _httpClient.Get(request);

                if (response.HasHttpError)
                {
                    return null;
                }

                var json = JsonDocument.Parse(response.Content);
                var container = json.RootElement.GetProperty("MediaContainer");

                if (container.TryGetProperty("Metadata", out var metadata))
                {
                    var enumerator = metadata.EnumerateArray().GetEnumerator();
                    if (enumerator.MoveNext())
                    {
                        return enumerator.Current;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, "Plex: Failed to get metadata for {0}", key);
            }

            return null;
        }

        private HttpRequest BuildPlexRequest(string path)
        {
            var separator = path.Contains('?') ? "&" : "?";
            var url = $"{_plexUrl}{path}{separator}X-Plex-Token={_plexToken}";

            var request = new HttpRequest(url);
            request.Headers.Accept = "application/json";
            request.SuppressHttpError = true;
            return request;
        }

        private string BuildPlexImageUrl(string imagePath)
        {
            return $"{_plexUrl}{imagePath}?X-Plex-Token={_plexToken}";
        }
    }
}
