using Playnite.SDK.Models;
using System;
using System.Collections.Generic;

namespace MutualGames.Models.Export;

public class ExternalGameData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public Guid PluginId { get; set; }
    public List<Guid> PlatformIds { get; set; }

    public static ExternalGameData FromGame(Game game)
    {
        return new ExternalGameData
        {
            Id = game.GameId,
            PluginId = game.PluginId,
            Name = game.Name,
            PlatformIds = game.PlatformIds,
        };
    }
}
