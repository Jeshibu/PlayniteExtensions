using Newtonsoft.Json;
using PlayniteExtensions.Common.Tests;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GamesSizeCalculator;

public class GamesSizeCalculatorSettings : ObservableObject
{
    public bool GetUninstalledGameSizeFromSteam{ get; set => SetValue(ref field, value); } = true;
    public bool GetSizeFromSteamNonSteamGames { get; set; }
    public bool IncludeDlcInSteamCalculation { get; set; }
    public bool IncludeOptionalInSteamCalculation { get; set; }

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public ObservableCollection<string> DepotRegionWords { get; set => SetValue(ref field, DeduplicateStrings(value)); } = ["eu", "europe", "row", "en", "english", "ww"];

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public ObservableCollection<string> DepotRegionWordsBlacklist { get; set => SetValue(ref field, DeduplicateStrings(value)); } =
    [
        "asia",
        "aus",
        "australia",
        "nz",
        "usa",
        "us",
        "ru",
        "russia",
        "germany",
        "deutschland",
        "de",
        "es",
        "sa",
        "cn",
        "china",
        "chinese",
        "schinese",
        "tchinese",
        "jp",
        "japanese",
        "fr",
        "french",
        "tr/mena",
        "sea",
        "tw"
    ];

    private static ObservableCollection<string> DeduplicateStrings(IEnumerable<string> strings)
    {
        var output = new ObservableCollection<string>(
            strings.Select(s => s.Trim().ToLowerInvariant())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct());
        return output;
    }
}

public class GamesSizeCalculatorSettingsViewModel : PluginSettingsViewModel<GamesSizeCalculatorSettings, GamesSizeCalculator>
{
    public string RegionWordsString
    {
        get => string.Join(Environment.NewLine, Settings.DepotRegionWords);
        set => Settings.DepotRegionWords = new(value.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries));
    }
    public string RegionWordsBlacklistString
    {
        get => string.Join(Environment.NewLine, Settings.DepotRegionWordsBlacklist);
        set => Settings.DepotRegionWordsBlacklist = new(value.Split([Environment.NewLine], StringSplitOptions.RemoveEmptyEntries));
    }

    public GamesSizeCalculatorSettingsViewModel(GamesSizeCalculator plugin):base(plugin, plugin.PlayniteApi)
    {
        Settings = LoadSavedSettings() ?? new GamesSizeCalculatorSettings();
    }
}
