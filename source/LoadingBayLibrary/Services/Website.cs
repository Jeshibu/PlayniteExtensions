using LoadingBayLibrary.Models;
using Newtonsoft.Json;
using PlayniteExtensions.Common;

namespace LoadingBayLibrary.Services;

public class Website(IWebDownloader downloader)
{
    public RecommendedResponseRoot GetRecommendedGames()
    {
        var response = downloader.DownloadString("https://api.loadingbay.com/app/v1/game_store/recommend/list");
        return JsonConvert.DeserializeObject<RecommendedResponseRoot>(response.ResponseContent);
    }
}

