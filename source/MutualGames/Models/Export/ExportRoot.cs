using System.Collections.Generic;

namespace MutualGames.Models.Export;

public class ExportRoot
{
    public List<ExternalGameData> Games { get; set; } = [];
    public List<PluginData> LibraryPlugins { get; set; } = [];
    public List<PlatformData> Platforms { get; set; } = [];
}