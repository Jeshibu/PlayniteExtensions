using Playnite.SDK;
using Playnite.SDK.Models;
using System;

namespace SteamCommon;

public static class Steam
{
    private static readonly ILogger logger = LogManager.GetLogger();
    private static Guid steamPluginId = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");

    public static bool IsGameSteamGame(Game game)
    {
        return game.PluginId == steamPluginId;
    }
}