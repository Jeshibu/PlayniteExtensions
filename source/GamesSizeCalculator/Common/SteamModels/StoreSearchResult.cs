using Playnite.SDK;

namespace GamesSizeCalculator.Common.SteamModels;

public class StoreSearchResult : GenericItemOption
{
    public string GameId { get; set; }
    public string StoreUrl { get; set; }
    public string BannerImageUrl { get; set; }
}
