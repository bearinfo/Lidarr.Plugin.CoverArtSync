# CoverArtSync — Lidarr Plugin

A metadata plugin for [Lidarr](https://lidarr.audio/) that automatically downloads artist and album artwork from multiple sources.

## Features

- **Multi-source artwork fetching** — aggregates images from up to 6 different providers
- **Artist images** — posters, fanart, logos, banners, clearlogos, thumbnails
- **Album images** — cover art, disc art
- **Per-source toggles** — enable/disable each source independently
- **Automatic deduplication** — one image per artwork type, first source wins

## Artwork Sources

| Source | Artist | Album | API Key Required |
|---|---|---|---|
| [Cover Art Archive](https://coverartarchive.org/) | — | ✅ | No |
| [Fanart.tv](https://fanart.tv/) | ✅ | — | Yes (free) |
| [TheAudioDB](https://www.theaudiodb.com/) | ✅ | ✅ | No |
| [Deezer](https://developers.deezer.com/) | ✅ | ✅ | No |
| [Spotify](https://developer.spotify.com/) | ✅ | ✅ | Yes (free) |
| [Plex](https://www.plex.tv/) | ✅ | ✅ | Plex token |

## Installation

In Lidarr, go to **System → Plugins** and install using:

```
https://github.com/bearinfo/Lidarr.Plugin.CoverArtSync
```

Restart Lidarr after installation.

## Configuration

After installation, go to **Settings → Metadata** and add **CoverArt Sync**.

### Image Types
- **Artist Images** — download artist artwork (poster, fanart, logo, banner)
- **Album Images** — download album cover art

### Sources

Enable the sources you want. Sources are checked in order and the first image found for each artwork type is used.

**Cover Art Archive** — Album covers via MusicBrainz release group IDs. No authentication needed.

**Fanart.tv** — Best source for artist artwork (posters, backgrounds, HD logos, banners). Requires a free API key from [fanart.tv](https://fanart.tv/get-an-api-key/).

**TheAudioDB** — Artist and album art keyed by MusicBrainz ID. No API key needed.

**Deezer** — Artist photos and album covers via public API. No authentication needed.

**Spotify** — High-resolution artist photos and album art. Requires Client ID and Client Secret from the [Spotify Developer Dashboard](https://developer.spotify.com/dashboard).

**Plex** — Fetch artwork from your local Plex Media Server. Requires your Plex URL and authentication token. Optionally specify a library section ID.

## Building from Source

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git with submodule support

### Build

```bash
git clone --recurse-submodules https://github.com/bearinfo/Lidarr.Plugin.CoverArtSync.git
cd Lidarr.Plugin.CoverArtSync
dotnet build CoverArtSync/CoverArtSync.csproj -c Release
```

The compiled plugin will be in `_plugins/CoverArtSync/`.

## License

This project is provided as-is for use with Lidarr.
