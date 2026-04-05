using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Extras.Metadata;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Validation;

namespace Lidarr.Plugin.CoverArtSync
{
    public class CoverArtSyncSettingsValidator : AbstractValidator<CoverArtSyncSettings>
    {
        public CoverArtSyncSettingsValidator()
        {
            RuleFor(x => x.PlexUrl)
                .NotEmpty()
                .When(x => x.EnablePlex)
                .WithMessage("Plex URL is required when Plex source is enabled");

            RuleFor(x => x.PlexToken)
                .NotEmpty()
                .When(x => x.EnablePlex)
                .WithMessage("Plex token is required when Plex source is enabled");

            RuleFor(x => x.FanartTvApiKey)
                .NotEmpty()
                .When(x => x.EnableFanartTv)
                .WithMessage("Fanart.tv API key is required when Fanart.tv source is enabled");

            RuleFor(x => x.SpotifyClientId)
                .NotEmpty()
                .When(x => x.EnableSpotify)
                .WithMessage("Spotify Client ID is required when Spotify source is enabled");

            RuleFor(x => x.SpotifyClientSecret)
                .NotEmpty()
                .When(x => x.EnableSpotify)
                .WithMessage("Spotify Client Secret is required when Spotify source is enabled");
        }
    }

    public class CoverArtSyncSettings : IProviderConfig
    {
        private static readonly CoverArtSyncSettingsValidator Validator = new();

        public CoverArtSyncSettings()
        {
            EnableArtistImages = true;
            EnableAlbumImages = true;
            EnableCoverArtArchive = true;
            EnableFanartTv = false;
            EnableTheAudioDb = false;
            EnablePlex = false;
            EnableSpotify = false;
            EnableDeezer = false;
            FanartTvApiKey = "";
            PlexUrl = "";
            PlexToken = "";
            PlexLibrarySection = "";
            SpotifyClientId = "";
            SpotifyClientSecret = "";
        }

        // --- Image Type Toggles ---

        [FieldDefinition(0, Label = "Artist Images", Type = FieldType.Checkbox, Section = MetadataSectionType.Image,
            HelpText = "Download artist artwork (poster, fanart, logo, banner)")]
        public bool EnableArtistImages { get; set; }

        [FieldDefinition(1, Label = "Album Images", Type = FieldType.Checkbox, Section = MetadataSectionType.Image,
            HelpText = "Download album cover art")]
        public bool EnableAlbumImages { get; set; }

        // --- Source Toggles (order = priority) ---

        [FieldDefinition(10, Label = "Cover Art Archive", Type = FieldType.Checkbox, Section = "Sources",
            HelpText = "MusicBrainz Cover Art Archive — album covers (uses MusicBrainz IDs). No API key needed.")]
        public bool EnableCoverArtArchive { get; set; }

        [FieldDefinition(11, Label = "Fanart.tv", Type = FieldType.Checkbox, Section = "Sources",
            HelpText = "Artist posters, backgrounds, HD logos, banners. Requires free API key.")]
        public bool EnableFanartTv { get; set; }

        [FieldDefinition(12, Label = "Fanart.tv API Key", Type = FieldType.Textbox, Section = "Sources",
            HelpText = "Get a free key at https://fanart.tv/get-an-api-key/", Privacy = PrivacyLevel.ApiKey)]
        public string FanartTvApiKey { get; set; }

        [FieldDefinition(13, Label = "TheAudioDB", Type = FieldType.Checkbox, Section = "Sources",
            HelpText = "Artist thumbnails and album art. Free, no API key needed for basic use.")]
        public bool EnableTheAudioDb { get; set; }

        [FieldDefinition(20, Label = "Plex", Type = FieldType.Checkbox, Section = "Sources",
            HelpText = "Fetch artwork from your local Plex instance.")]
        public bool EnablePlex { get; set; }

        [FieldDefinition(21, Label = "Plex URL", Type = FieldType.Textbox, Section = "Sources",
            HelpText = "e.g. http://localhost:32400")]
        public string PlexUrl { get; set; }

        [FieldDefinition(22, Label = "Plex Token", Type = FieldType.Password, Section = "Sources",
            HelpText = "Your Plex authentication token", Privacy = PrivacyLevel.Password)]
        public string PlexToken { get; set; }

        [FieldDefinition(23, Label = "Plex Music Library Section", Type = FieldType.Textbox, Section = "Sources",
            HelpText = "Library section ID (leave blank to auto-detect music libraries)")]
        public string PlexLibrarySection { get; set; }

        [FieldDefinition(30, Label = "Spotify", Type = FieldType.Checkbox, Section = "Sources",
            HelpText = "High-resolution artist photos. Requires Spotify API credentials.")]
        public bool EnableSpotify { get; set; }

        [FieldDefinition(31, Label = "Spotify Client ID", Type = FieldType.Textbox, Section = "Sources",
            HelpText = "From https://developer.spotify.com/dashboard", Privacy = PrivacyLevel.ApiKey)]
        public string SpotifyClientId { get; set; }

        [FieldDefinition(32, Label = "Spotify Client Secret", Type = FieldType.Password, Section = "Sources",
            HelpText = "From Spotify developer dashboard", Privacy = PrivacyLevel.Password)]
        public string SpotifyClientSecret { get; set; }

        [FieldDefinition(33, Label = "Deezer", Type = FieldType.Checkbox, Section = "Sources",
            HelpText = "Artist and album art. Free, no API key needed.")]
        public bool EnableDeezer { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }
}
