using NzbDrone.Core.Plugins;

namespace Lidarr.Plugin.CoverArtSync
{
    public class CoverArtSyncPlugin : NzbDrone.Core.Plugins.Plugin
    {
        public override string Name => "CoverArtSync";
        public override string Owner => "bearinfo";
        public override string GithubUrl => "https://github.com/bearinfo/Lidarr.Plugin.CoverArtSync";
    }
}
