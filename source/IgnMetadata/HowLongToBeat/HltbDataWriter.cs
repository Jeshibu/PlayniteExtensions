using IgnMetadata.HowLongToBeat.Models;
using Newtonsoft.Json;
using Playnite.SDK.Models;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace IgnMetadata.HowLongToBeat;

public class HltbDataWriter(string pluginDataFolderRoot)
{
    private const string HltbPluginId = "e08cd51f-9c9a-4ee3-a094-fde03b55492f";
    private string GetFilePath(Guid gameId) => Path.Combine(pluginDataFolderRoot, HltbPluginId, "HowLongToBeat", $"{gameId}.json");
    public bool HasHltbData(Game game) => File.Exists(GetFilePath(game.Id));
    public bool HasNoHltbData(Game game) => !HasHltbData(game);

    public bool WriteHltbData(GameHowLongToBeat data)
    {
        if (!data.Items.Any(i => i.GameHltbData.MainStoryClassic > 0 || i.GameHltbData.MainExtraClassic > 0 || i.GameHltbData.CompletionistClassic > 0))
            return false;

        string path = GetFilePath(data.Id);
        string dataString = JsonConvert.SerializeObject(data, Formatting.None);
        File.WriteAllText(path, dataString, Encoding.UTF8);
        return true;
    }

    public bool WriteHltbData(IgnHltbDataModel data, Game game) => WriteHltbData(Convert(data, game));

    private static GameHowLongToBeat Convert(IgnHltbDataModel data, Game game)
    {
        var hltbIdUtility = new HltbIdUtility();

        var timeData = new HltbData
        {
            MainStoryClassic = data.MainStoryHours * 60 * 60,
            MainExtraClassic = data.MainStoryAndSidesHours * 60 * 60,
            CompletionistClassic = data.EverythingHours * 60 * 60,
        };

        var gameData = new HltbDataUser
        {
            GameType = GameType.Game,
            Id = hltbIdUtility.GetIdFromUrl(data.HltbUrl).Id,
            Name = data.Name,
            Url = data.HltbUrl,
            UrlImg = data.CoverUrl,
            Platform = string.Join(", ", data.Platforms),
            GameHltbData = timeData,
            IsVndb = false,
        };

        return new()
        {
            Id = game.Id,
            Name = game.Name,
            DateLastRefresh = DateTime.Now,
            Items = [gameData]
        };
    }
}
