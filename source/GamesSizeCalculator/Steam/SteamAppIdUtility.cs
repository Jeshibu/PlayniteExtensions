using GamesSizeCalculator.Models;
using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using SteamCommon;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GamesSizeCalculator.SteamSizeCalculation;

public class SteamAppIdUtility(ICachedFile steamAppList) : ISteamAppIdUtility
{
    private static readonly Guid SteamLibraryPluginId = Guid.Parse("CB91DFC9-B977-43BF-8E70-55F46E410FAB");
    private static readonly Regex SteamUrlRegex = new Regex(@"\bhttps?://st(ore\.steampowered|eamcommunity)\.com/app/(?<id>[0-9]+)", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
    private static readonly Regex NonLetterOrDigitCharacterRegex = new Regex(@"[^\p{L}\p{Nd}]", RegexOptions.Compiled);
    private static readonly ILogger logger = LogManager.GetLogger();

    private Dictionary<string, int> _steamIds;
    private Dictionary<string, int> SteamIdsByTitle
    {
        get { return _steamIds ??= GetSteamIdsByTitle(); }
    }

    public ICachedFile SteamAppList { get; } = steamAppList;

    private static string NormalizeTitle(string title)
    {
        return NonLetterOrDigitCharacterRegex.Replace(title, string.Empty).ToLower();
    }

    public string GetSteamGameId(Game game)
    {
        if (game.PluginId == SteamLibraryPluginId)
        {
            return game.GameId;
        }

        if (game.Links != null)
        {
            foreach (var link in game.Links)
            {
                var match = SteamUrlRegex.Match(link.Url);
                if (match.Success)
                {
                    return match.Groups["id"].Value;
                }
            }
        }

        if (SteamIdsByTitle.TryGetValue(NormalizeTitle(game.Name), out int appId))
        {
            return appId.ToString();
        }

        return SteamWeb.GetSteamIdFromSearch(game.Name);
    }

    private Dictionary<string, int> GetSteamIdsByTitle()
    {
        var jsonStr = SteamAppList.GetFileContents();
        var jsonContent = Serialization.FromJson<SteamAppListRoot>(jsonStr);
        Dictionary<string, int> output = [];
        foreach (var app in jsonContent.Applist.Apps)
        {
            var normalizedTitle = NormalizeTitle(app.Name);
            if (output.ContainsKey(normalizedTitle))
            {
                continue;
            }

            output.Add(normalizedTitle, app.Appid);
        }

        return output;
    }
}