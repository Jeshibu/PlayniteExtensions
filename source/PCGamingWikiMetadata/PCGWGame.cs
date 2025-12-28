using System;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using PCGamingWikiBulkImport;

namespace PCGamingWikiMetadata;

public class PcgwGame(PCGamingWikiMetadataSettings settings) : GenericItemOption
{
    private readonly ILogger _logger = LogManager.GetLogger();
    public int PageId { get; set; }

    public List<MetadataProperty> Genres { get; } = [];
    public List<MetadataProperty> Developers { get; } = [];
    public List<MetadataProperty> Publishers { get; } = [];
    public List<MetadataProperty> Features { get; } = [];
    public List<MetadataProperty> Series { get; } = [];

    public List<Link> Links { get; } = [];
    public List<MetadataProperty> Tags { get; } = [];

    private readonly IDictionary<string, int?> _reception = new Dictionary<string, int?>();
    private readonly IDictionary<string, ReleaseDate?> _releaseDates = new Dictionary<string, ReleaseDate?>();

    public Game LibraryGame;

    public PcgwGame(PCGamingWikiMetadataSettings settings, string name, int pageid) : this(settings)
    {
        Name = name;
        PageId = pageid;
    }

    public Link PcGamingWikiLink()
    {
        string url = Name.TitleToSlug().SlugToUrl();
        return new Link("PCGamingWiki", url);
    }

    public void AddReception(string aggregator, int score)
    {
        _reception[aggregator] = score;
    }

    public bool GetOpenCriticReception(out int? score)
    {
        return GetReception("OpenCritic", out score);
    }

    public bool GetIgdbReception(out int? score)
    {
        return GetReception("IGDB", out score);
    }
    public bool GetMetacriticReception(out int? score)
    {
        return GetReception("Metacritic", out score);
    }

    protected bool GetReception(string aggregator, out int? score)
    {
        return _reception.TryGetValue(aggregator, out score);
    }

    public void AddFullControllerSupport(string description)
    {
        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            AddFeature("Full Controller Support");
        }
    }

    public void AddPlayStationControllerSupport(string description)
    {
        if (!settings.ImportFeaturePlayStationControllers)
        {
            return;
        }

        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            AddFeature("PlayStation Controller");
        }
    }

    public void AddPlayStationButtonPrompts(string description)
    {
        if (!settings.ImportFeaturePlayStationButtonPrompts)
        {
            return;
        }

        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            AddFeature("PlayStation Button Prompts");
        }
    }

    public void AddLightBarSupport(string description)
    {
        if (!settings.ImportFeatureLightBar)
        {
            return;
        }

        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            AddFeature("Light Bar Support");
        }
    }

    public void AddAdaptiveTriggerSupport(string description)
    {
        if (!settings.ImportFeatureAdaptiveTrigger)
        {
            return;
        }

        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            AddFeature("Adaptive Trigger Support");
        }
    }

    public void AddHapticFeedbackSupport(string description)
    {
        if (!settings.ImportFeatureHapticFeedback)
        {
            return;
        }

        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            AddFeature("Haptic Feedback Support");
        }
    }

    public void AddTouchscreenSupport(string description)
    {
        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            AddFeature("Touchscreen optimised");
        }
    }

    public void AddControllerSupport(string description)
    {
        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            AddFeature("Controller Support");
        }
    }

    public void SetFramerate60()
    {
        AddFeature("60 FPS");
    }

    public void SetFramerate120Plus()
    {
        AddFeature("120+ FPS");
    }

    private void AddMultiplayerFeatures(string rating, string featureBaseName, short playerCount, IList<string> types)
    {
        if (rating != PCGamingWikiType.Rating.NativeSupport)
        {
            return;
        }

        foreach (string type in types)
        {
            AddFeature($"{featureBaseName}: {type}");
        }

        if (playerCount == PcGamingWikiHtmlParser.UndefinedPlayerCount)
        {
            AddFeature(featureBaseName);
            return;
        }

        if (playerCount == 2)
        {
            AddFeature($"{featureBaseName}: 2");
        }
        else if (playerCount > 2 && playerCount <= 4)
        {
            AddFeature($"{featureBaseName}: 2-4");
        }
        else if (playerCount > 4 && playerCount <= 8)
        {
            AddFeature($"{featureBaseName}: 4-8");
        }
        else if (playerCount > 8)
        {
            AddFeature($"{featureBaseName}: 8+");
        }
    }

    public void AddMultiplayerLocal(string rating, short playerCount, IList<string> types)
    {
        AddMultiplayerFeatures(rating, "Local Multiplayer", playerCount, types);
    }

    public void AddMultiplayerLan(string rating, short playerCount, IList<string> types)
    {
        AddMultiplayerFeatures(rating, "LAN Multiplayer", playerCount, types);
    }

    public void AddMultiplayerOnline(string rating, short playerCount, IList<string> types)
    {
        AddMultiplayerFeatures(rating, "Online Multiplayer", playerCount, types);
    }

    public void AddMultiplayerAsynchronous(string rating, short playerCount, IList<string> types)
    {
        AddMultiplayerFeatures(rating, "Asynchronous Multiplayer", playerCount, types);
    }

    public void AddTag(string t)
    {
        Tags.Add(new MetadataNameProperty(t));
    }

    public void AddFeature(string t)
    {
        Features.AddMissing(new MetadataNameProperty(t));
    }

    public void AddSeries(string t)
    {
        Series.Add(new MetadataNameProperty(t));
    }

    public void AddCsvSeries(string csv)
    {
        string[] tags = SplitCsvString(csv);

        foreach (string tag in tags)
        {
            AddSeries(tag);
        }
    }

    public void AddCsvFeatures(string csv)
    {
        string[] tags = SplitCsvString(csv);

        foreach (string tag in tags)
        {
            AddFeature(tag);
        }
    }

    public void AddCsvTags(string csv, string prefix)
    {
        string[] tags = SplitCsvString(csv);

        foreach (string tag in tags)
        {
            string tagString = $"{prefix} {tag}";
            AddTag(tagString.Trim());
        }
    }

    public void AddCsvTags(string csv)
    {
        string[] tags = SplitCsvString(csv);

        foreach (string tag in tags)
        {
            AddTag(tag);
        }
    }

    public ReleaseDate? WindowsReleaseDate()
    {

        if (_releaseDates.TryGetValue("Windows", out ReleaseDate? date))
        {
            return date;
        }
        else
        {
            return null;
        }
    }

    public void AddReleaseDate(string platform, DateTime date)
    {
        _releaseDates[platform] = new ReleaseDate(date);
    }

    public string[] SplitCsvString(string csv)
    {
        TextFieldParser parser = new(new StringReader(csv));
        parser.SetDelimiters(",");
        return parser.ReadFields();
    }

    public void AddGenres(string genreCsv)
    {
        string[] genres = SplitCsvString(genreCsv);

        foreach (string genre in genres)
        {
            Genres.Add(new MetadataNameProperty(genre));
        }
    }

    public void SetXboxPlayAnywhere()
    {
        if (BuiltinExtensions.GetExtensionFromId(LibraryGame.PluginId) == BuiltinExtension.XboxLibrary)
        {
            AddFeature("Xbox Play Anywhere");
        }
    }

    public void AddVrFeature() => AddFeature("VR");

    public void AddMiddleware(string type, string name)
    {
        if (!settings.ImportTagMiddleware)
            return;

        var prefix = $"{settings.TagPrefixMiddleware} {type}:".Trim();
        AddCsvTags(name, prefix);
    }
}
