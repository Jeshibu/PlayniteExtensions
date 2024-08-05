using System.Collections.Generic;

namespace MutualGames.Models.Export
{
    public class ExportRoot
    {
        public List<ExternalGameData> Games { get; set; } = new List<ExternalGameData>();
        public List<PluginData> LibraryPlugins { get; set; } = new List<PluginData>();
        public List<PlatformData> Platforms { get; set; } = new List<PlatformData>();
    }
}