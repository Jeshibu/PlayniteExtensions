namespace GamesSizeCalculator.Steam.Models;

public class AppListResponseRoot
{
    public AppListResponse response { get; set; }
}

public class AppListResponse
{
    public SteamAppInfo[] apps { get; set; }
    public bool have_more_results { get; set; }
    public long last_appid { get; set; }
}

public class SteamAppInfo
{
    public long appid { get; set; }
    public string name { get; set; }
    public long last_modified { get; set; }
    public long price_change_number { get; set; }
}
