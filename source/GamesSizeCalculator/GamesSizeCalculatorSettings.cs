using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace GamesSizeCalculator;

public class GamesSizeCalculatorSettings : ObservableObject
{
    private bool getUninstalledGameSizeFromSteam = true;
    public bool GetUninstalledGameSizeFromSteam { get => getUninstalledGameSizeFromSteam; set => SetValue(ref getUninstalledGameSizeFromSteam, value); }
    public bool GetSizeFromSteamNonSteamGames { get; set; }
    public bool IncludeDlcInSteamCalculation { get; set; }
    public bool IncludeOptionalInSteamCalculation { get; set; }

    private ObservableCollection<string> depotRegionWords = ["eu", "europe", "row", "en", "english", "ww"];

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public ObservableCollection<string> DepotRegionWords { get => depotRegionWords; set => SetValue(ref depotRegionWords, DeduplicateStrings(value)); }

    private ObservableCollection<string> depotRegionWordsBlacklist =
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

    [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
    public ObservableCollection<string> DepotRegionWordsBlacklist { get => depotRegionWordsBlacklist; set => SetValue(ref depotRegionWordsBlacklist, DeduplicateStrings(value)); }

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
        set => Settings.DepotRegionWords = new ObservableCollection<string>(value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
    }
    public string RegionWordsBlacklistString
    {
        get => string.Join(Environment.NewLine, Settings.DepotRegionWordsBlacklist);
        set => Settings.DepotRegionWordsBlacklist = new ObservableCollection<string>(value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries));
    }

    public GamesSizeCalculatorSettingsViewModel(GamesSizeCalculator plugin):base(plugin, plugin.PlayniteApi)
    {
        // Load saved settings.
        var savedSettings = plugin.LoadPluginSettings<GamesSizeCalculatorSettings>();

        // LoadPluginSettings returns null if not saved data is available.
        if (savedSettings != null)
        {
            Settings = savedSettings;
        }
        else
        {
            Settings = new GamesSizeCalculatorSettings();
        }
    }
}