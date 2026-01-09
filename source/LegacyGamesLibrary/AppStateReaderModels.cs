using Newtonsoft.Json;
using System.Collections.Generic;

namespace LegacyGamesLibrary;

internal class AppStateRoot
{
    public AppStateSiteData SiteData { get; set; }
    public AppStateUser User { get; set; }
}

internal class AppStateSiteData
{
    public List<AppStateBundle> Catalog { get; set; }
    public List<AppStateBundle> GiveawayCatalog { get; set; }
}

internal class AppStateBundle
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Permalink { get; set; }
    public string Description { get; set; }
    public List<AppStateGame> Games { get; set; } = [];
}

internal class AppStateUser
{
    public List<AppStateUserDownloadLicense> GiveawayDownloads { get; set; } = [];
    public AppStateUserProfile Profile { get; set; }
}

internal class AppStateUserProfile
{
    public List<AppStateUserDownloadLicense> Downloads { get; set; } = [];
}

internal class AppStateUserDownloadLicense
{
    [JsonProperty("product_id")]
    public int ProductId { get; set; }
}

public class AppStateGame
{
    [JsonProperty("game_name")]
    public string GameName { get; set; }
    [JsonProperty("game_description")]
    public string GameDescription { get; set; }
    [JsonProperty("game_coverart")]
    public string GameCoverArt { get; set; }
    [JsonProperty("game_installed_size")]
    public string GameInstalledSize { get; set; }
    [JsonProperty("installer_uuid")]
    public string InstallerUUID { get; set; }
}
