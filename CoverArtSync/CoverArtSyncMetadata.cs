using System.IO;
using NLog;
using NzbDrone.Common.Http;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using Lidarr.Plugin.CoverArtSync.Sources;

namespace Lidarr.Plugin.CoverArtSync
{
    public class CoverArtSyncMetadata : MetadataBase<CoverArtSyncSettings>
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;

        public override string Name => "CoverArt Sync";

        public CoverArtSyncMetadata(IHttpClient httpClient, Logger logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public override MetadataFile FindMetadataFile(Artist artist, string path)
        {
            // We don't write XML/NFO metadata, only images
            return null!;
        }

        public override MetadataFileResult ArtistMetadata(Artist artist)
        {
            // No text metadata, only images
            return null!;
        }

        public override MetadataFileResult AlbumMetadata(Artist artist, Album album, string albumPath)
        {
            return null!;
        }

        public override MetadataFileResult TrackMetadata(Artist artist, TrackFile trackFile)
        {
            return null!;
        }

        public override List<ImageFileResult> ArtistImages(Artist artist)
        {
            if (!Settings.EnableArtistImages)
            {
                return new List<ImageFileResult>();
            }

            var musicBrainzId = artist.Metadata.Value.ForeignArtistId;
            var artistName = artist.Metadata.Value.Name;

            _logger.Debug("CoverArtSync: Fetching artist images for {0} ({1})", artistName, musicBrainzId);

            var allImages = new List<ArtworkImage>();
            var sources = BuildArtistSources();

            foreach (var source in sources)
            {
                try
                {
                    var images = source.GetArtistImages(musicBrainzId, artistName);
                    if (images.Count > 0)
                    {
                        _logger.Debug("CoverArtSync: {0} returned {1} artist images for {2}",
                            source.Name, images.Count, artistName);
                        allImages.AddRange(images);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "CoverArtSync: Source {0} failed for artist {1}", source.Name, artistName);
                }
            }

            return ConvertToArtistImageResults(allImages);
        }

        public override List<ImageFileResult> AlbumImages(Artist artist, Album album, string albumPath)
        {
            if (!Settings.EnableAlbumImages)
            {
                return new List<ImageFileResult>();
            }

            var musicBrainzAlbumId = album.ForeignAlbumId;
            var artistName = artist.Metadata.Value.Name;
            var albumTitle = album.Title;

            _logger.Debug("CoverArtSync: Fetching album images for {0} - {1} ({2})",
                artistName, albumTitle, musicBrainzAlbumId);

            var allImages = new List<ArtworkImage>();
            var sources = BuildAlbumSources();

            foreach (var source in sources)
            {
                try
                {
                    var images = source.GetAlbumImages(musicBrainzAlbumId, albumTitle, artistName);
                    if (images.Count > 0)
                    {
                        _logger.Debug("CoverArtSync: {0} returned {1} album images for {2}",
                            source.Name, images.Count, albumTitle);
                        allImages.AddRange(images);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "CoverArtSync: Source {0} failed for album {1}", source.Name, albumTitle);
                }
            }

            return ConvertToAlbumImageResults(allImages, albumPath);
        }

        public override List<ImageFileResult> TrackImages(Artist artist, TrackFile trackFile)
        {
            return new List<ImageFileResult>();
        }

        private List<IArtworkSource> BuildArtistSources()
        {
            var sources = new List<IArtworkSource>();

            // Priority order matches UI ordering
            if (Settings.EnableCoverArtArchive)
            {
                sources.Add(new CoverArtArchiveSource(_httpClient, _logger));
            }

            if (Settings.EnableFanartTv && !string.IsNullOrWhiteSpace(Settings.FanartTvApiKey))
            {
                sources.Add(new FanartTvSource(_httpClient, _logger, Settings.FanartTvApiKey));
            }

            if (Settings.EnableTheAudioDb)
            {
                sources.Add(new TheAudioDbSource(_httpClient, _logger));
            }

            if (Settings.EnablePlex &&
                !string.IsNullOrWhiteSpace(Settings.PlexUrl) &&
                !string.IsNullOrWhiteSpace(Settings.PlexToken))
            {
                sources.Add(new PlexArtworkSource(_httpClient, _logger,
                    Settings.PlexUrl, Settings.PlexToken, Settings.PlexLibrarySection));
            }

            if (Settings.EnableSpotify &&
                !string.IsNullOrWhiteSpace(Settings.SpotifyClientId) &&
                !string.IsNullOrWhiteSpace(Settings.SpotifyClientSecret))
            {
                sources.Add(new SpotifyArtworkSource(_httpClient, _logger,
                    Settings.SpotifyClientId, Settings.SpotifyClientSecret));
            }

            if (Settings.EnableDeezer)
            {
                sources.Add(new DeezerArtworkSource(_httpClient, _logger));
            }

            return sources;
        }

        private List<IArtworkSource> BuildAlbumSources()
        {
            // Same sources work for albums, priority order stays the same
            return BuildArtistSources();
        }

        private static List<ImageFileResult> ConvertToArtistImageResults(List<ArtworkImage> images)
        {
            var results = new List<ImageFileResult>();
            var usedTypes = new HashSet<ArtworkType>();

            foreach (var image in images)
            {
                // Take only the first (highest priority) image per type
                if (!usedTypes.Add(image.Type))
                {
                    continue;
                }

                var filename = image.Type switch
                {
                    ArtworkType.Poster => "folder",
                    ArtworkType.Fanart => "fanart",
                    ArtworkType.Banner => "banner",
                    ArtworkType.Logo => "logo",
                    ArtworkType.Clearlogo => "clearlogo",
                    ArtworkType.Thumb => "thumb",
                    _ => null
                };

                if (filename == null)
                {
                    continue;
                }

                var extension = GetExtension(image.Url);
                results.Add(new ImageFileResult($"{filename}{extension}", image.Url));
            }

            return results;
        }

        private static List<ImageFileResult> ConvertToAlbumImageResults(List<ArtworkImage> images, string albumPath)
        {
            var results = new List<ImageFileResult>();
            var usedTypes = new HashSet<ArtworkType>();

            foreach (var image in images)
            {
                if (!usedTypes.Add(image.Type))
                {
                    continue;
                }

                var filename = image.Type switch
                {
                    ArtworkType.Cover => "cover",
                    ArtworkType.Disc => "discart",
                    ArtworkType.Poster => "folder",
                    _ => null
                };

                if (filename == null)
                {
                    continue;
                }

                var extension = GetExtension(image.Url);
                var relativePath = Path.Combine(albumPath, $"{filename}{extension}");
                results.Add(new ImageFileResult(relativePath, image.Url));
            }

            return results;
        }

        private static string GetExtension(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var ext = Path.GetExtension(path);

                if (!string.IsNullOrEmpty(ext) &&
                    (ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
                     ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
                     ext.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                     ext.Equals(".webp", StringComparison.OrdinalIgnoreCase)))
                {
                    return ext;
                }
            }
            catch
            {
                // Ignore URI parsing errors
            }

            return ".jpg";
        }
    }
}
