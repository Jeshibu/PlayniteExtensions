using Newtonsoft.Json;
using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace MobyGamesMetadata;

public class MobyGamesMetadataSettings : BulkImportPluginSettings
{
    public DataSource DataSource { get; set; } = DataSource.Api;
    public string ApiKey { get; set => SetValue(ref field, value?.Trim()); }
    public bool ShowTopPanelButton { get; set; } = true;
    public ObservableCollection<MobyGamesGenreSetting> Genres { get; set; } = [];

    public MobyGamesImageSourceSettings Cover { get; set; } = new()
    {
        MinWidth = 200,
        MinHeight = 300,
        AspectRatio = AspectRatio.Vertical,
    };

    public MobyGamesImageSourceSettings Background { get; set; } = new()
    {
        MinWidth = 900,
        MinHeight = 600,
        AspectRatio = AspectRatio.Any,
    };

    public bool MatchPlatformsForReleaseDate { get; set; } = false;
    public bool MatchPlatformsForDevelopers { get; set; } = true;
    public bool MatchPlatformsForPublishers { get; set; } = true;

    [Obsolete]
    public ReleaseDateSource ReleaseDateSource { get; set; } = ReleaseDateSource.EarliestOverall;
}

public class MobyGamesGenreSetting : ObservableObject, IHasName
{
    [JsonProperty("genre_category_id")]
    public int CategoryId { get; set; }

    [JsonProperty("genre_category")]
    public string Category { get; set; }

    [JsonProperty("genre_id")]
    public int Id { get; set; }

    [JsonProperty("genre_name")]
    public string Name { get; set; }

    public string NameOverride{ get; set => SetValue(ref field, value); }

    public PropertyImportTarget ImportTarget{ get; set => SetValue(ref field, value); } = PropertyImportTarget.Genres;
}

internal class MobyGamesGenresRoot
{
    public List<MobyGamesGenreSetting> Genres { get; set; }
}

public enum AspectRatio
{
    Any,
    Vertical,
    Horizontal,
    Square,
}

public class MobyGamesImageSourceSettings
{
    public int MinHeight { get; set; }
    public int MinWidth { get; set; }
    public AspectRatio AspectRatio { get; set; }
    public bool MatchPlatforms { get; set; }
}

public enum ReleaseDateSource
{
    EarliestOverall,
    EarliestForAutomaticallyMatchedPlatform,
}

[Flags]
public enum DataSource
{
    None = 0,
    Api = 1,
    Scraping = 2,
    ApiAndScraping = 3,
}

public class MobyGamesMetadataSettingsViewModel : PluginSettingsViewModel<MobyGamesMetadataSettings, MobyGamesMetadata>
{
    public MobyGamesMetadataSettingsViewModel(MobyGamesMetadata plugin) : base(plugin, plugin.PlayniteApi)
    {
        // Load saved settings.
        Settings = LoadSavedSettings();

        // LoadPluginSettings returns null if no saved data is available.
        if (Settings != null)
        {
            UpgradeSettings();
        }
        else
        {
            Settings = new() { Version = 1 };
        }
        InitializeGenres();
    }

    public RelayCommand<object> GetApiKeyCommand => new(_ => { Process.Start(@"https://www.mobygames.com/info/api/"); });

    public PropertyImportTarget[] ImportTargets { get; } =
    [
        PropertyImportTarget.Ignore,
        PropertyImportTarget.Genres,
        PropertyImportTarget.Tags,
        PropertyImportTarget.Features,
    ];

    public AspectRatio[] AspectRatios { get; } =
    [
        AspectRatio.Any,
        AspectRatio.Vertical,
        AspectRatio.Horizontal,
        AspectRatio.Square,
    ];

    private void InitializeGenres()
    {
        var assemblyLocation = System.Reflection.Assembly.GetExecutingAssembly().Location;
        var genresPath = Path.Combine(Path.GetDirectoryName(assemblyLocation), "genres.json");
        var genresContent = File.ReadAllText(genresPath);
        var root = JsonConvert.DeserializeObject<MobyGamesGenresRoot>(genresContent);
        bool genreAdded = false;
        foreach (var newGenre in root.Genres)
        {
            var existingGenre = Settings.Genres.FirstOrDefault(g => g.Id == newGenre.Id);
            if (existingGenre != null)
            {
                existingGenre.CategoryId = newGenre.CategoryId;
                existingGenre.Category = newGenre.Category;
                existingGenre.Name = newGenre.Name;
            }
            else
            {
                newGenre.ImportTarget = GetDefaultImportTarget(newGenre);
                Settings.Genres.Add(newGenre);
                genreAdded = true;
            }
        }
        if (genreAdded)
        {
            var orderedGenres = Settings.Genres.OrderBy(g => g.Category).ThenBy(g => g.Name).ToList();
            Settings.Genres.Clear();
            foreach (var g in orderedGenres)
            {
                Settings.Genres.Add(g);
            }
        }
        Logger.Debug($"{Settings.Genres.Count} genres");
    }

    private PropertyImportTarget GetDefaultImportTarget(MobyGamesGenreSetting genre)
    {
        switch (genre.CategoryId)
        {
            case 1: //Basic Genres
            case 2: //Perspective
            case 4: //Gameplay
                return PropertyImportTarget.Genres;
            case 14: //DLC/Add-on
            case 15: //Special Edition
                return PropertyImportTarget.Ignore;
            case 3: //Sports Themes
            case 5: //Educational Categories
            case 6: //Other Attributes
            case 7: //Interface/Control
            case 8: //Narrative Theme/Topic
            case 9: //Pacing
            case 10: //Setting
            case 11: //Vehicular Themes
            case 12: //Visual Presentation
            case 13: //Art Style
            default:
                return PropertyImportTarget.Tags;
        }
    }

    public void SetImportTarget(PropertyImportTarget target, ICollection<MobyGamesGenreSetting> settings)
    {
        if (settings == null)
            return;

        foreach (var s in settings)
        {
            s.ImportTarget = target;
        }
    }

    private void UpgradeSettings()
    {
        #pragma warning disable CS0612 // Type or member is obsolete

        if (Settings.Version < 1)
            Settings.MaxDegreeOfParallelism = BulkImportPluginSettings.GetDefaultMaxDegreeOfParallelism();

        if (Settings.Version < 2)
            Settings.MatchPlatformsForReleaseDate = Settings.ReleaseDateSource == ReleaseDateSource.EarliestForAutomaticallyMatchedPlatform;

        if (Settings.Version < 3 && Settings.DataSource == DataSource.ApiAndScraping)
            Settings.DataSource = DataSource.Api;

        #pragma warning restore CS0612 // Type or member is obsolete

        Settings.Version = 3;
    }
}
