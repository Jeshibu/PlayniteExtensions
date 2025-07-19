using System;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using System.IO;
using PCGamingWikiBulkImport;

namespace PCGamingWikiMetadata;

public class PCGWGame : GenericItemOption
{
    private readonly ILogger logger = LogManager.GetLogger();
    public int PageID { get; set; }

    private PCGamingWikiMetadataSettings settings;

    public List<MetadataProperty> Genres { get; }
    public List<MetadataProperty> Developers { get; }
    public List<MetadataProperty> Publishers { get; }
    public List<MetadataProperty> Features { get; }
    public List<MetadataProperty> Series { get; }

    public List<Link> Links { get; }
    public List<MetadataProperty> Tags { get; }

    private IDictionary<string, int?> reception;
    private IDictionary<string, ReleaseDate?> ReleaseDates;

    public Game LibraryGame;

    public PCGWGame(PCGamingWikiMetadataSettings settings)
    {
        this.settings = settings;
        this.Links = [];
        this.Genres = [];
        this.Features = [];
        this.Series = [];
        this.Developers = [];
        this.Publishers = [];
        this.Tags = [];
        this.ReleaseDates = new Dictionary<string, ReleaseDate?>();
        this.reception = new Dictionary<string, int?>();
    }

    public PCGWGame(PCGamingWikiMetadataSettings settings, string name, int pageid) : this(settings)
    {
        this.Name = name;
        this.PageID = pageid;
    }

    public Link PCGamingWikiLink()
    {
        string url = this.Name.TitleToSlug().SlugToUrl();
        return new Link("PCGamingWiki", url);
    }

    public void AddReception(string aggregator, int score)
    {
        this.reception[aggregator] = score;
    }

    public bool GetOpenCriticReception(out int? score)
    {
        return GetReception("OpenCritic", out score);
    }

    public bool GetIGDBReception(out int? score)
    {
        return GetReception("IGDB", out score);
    }
    public bool GetMetacriticReception(out int? score)
    {
        return GetReception("Metacritic", out score);
    }

    protected bool GetReception(string aggregator, out int? score)
    {
        return this.reception.TryGetValue(aggregator, out score);
    }

    public void AddFullControllerSupport(string description)
    {
        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            this.AddFeature("Full Controller Support");
        }
    }

    public void AddPlayStationControllerSupport(string description)
    {
        if (!this.settings.ImportFeaturePlayStationControllers)
        {
            return;
        }

        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            this.AddFeature("PlayStation Controller");
        }
    }

    public void AddPlayStationButtonPrompts(string description)
    {
        if (!this.settings.ImportFeaturePlayStationButtonPrompts)
        {
            return;
        }

        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            this.AddFeature("PlayStation Button Prompts");
        }
    }

    public void AddLightBarSupport(string description)
    {
        if (!this.settings.ImportFeatureLightBar)
        {
            return;
        }

        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            this.AddFeature("Light Bar Support");
        }
    }

    public void AddAdaptiveTriggerSupport(string description)
    {
        if (!this.settings.ImportFeatureAdaptiveTrigger)
        {
            return;
        }

        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            this.AddFeature("Adaptive Trigger Support");
        }
    }

    public void AddHapticFeedbackSupport(string description)
    {
        if (!this.settings.ImportFeatureHapticFeedback)
        {
            return;
        }

        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            this.AddFeature("Haptic Feedback Support");
        }
    }

    public void AddTouchscreenSupport(string description)
    {
        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            this.AddFeature("Touchscreen optimised");
        }
    }

    public void AddControllerSupport(string description)
    {
        if (description == PCGamingWikiType.Rating.NativeSupport)
        {
            this.AddFeature("Controller Support");
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
            this.AddFeature($"{featureBaseName}: {type}");
        }

        if (playerCount == PCGamingWikiHTMLParser.UndefinedPlayerCount)
        {
            this.AddFeature(featureBaseName);
            return;
        }

        if (playerCount == 2)
        {
            this.AddFeature($"{featureBaseName}: 2");
        }
        else if (playerCount > 2 && playerCount <= 4)
        {
            this.AddFeature($"{featureBaseName}: 2-4");
        }
        else if (playerCount > 4 && playerCount <= 8)
        {
            this.AddFeature($"{featureBaseName}: 4-8");
        }
        else if (playerCount > 8)
        {
            this.AddFeature($"{featureBaseName}: 8+");
        }
    }

    public void AddMultiplayerLocal(string rating, short playerCount, IList<string> types)
    {
        AddMultiplayerFeatures(rating, "Local Multiplayer", playerCount, types);
    }

    public void AddMultiplayerLAN(string rating, short playerCount, IList<string> types)
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
        this.Tags.Add(new MetadataNameProperty(t));
    }

    public void AddFeature(string t)
    {
        this.Features.AddMissing(new MetadataNameProperty(t));
    }

    public void AddSeries(string t)
    {
        this.Series.Add(new MetadataNameProperty(t));
    }

    public void AddCSVSeries(string csv)
    {
        string[] tags = SplitCSVString(csv);

        foreach (string tag in tags)
        {
            AddSeries(tag);
        }
    }

    public void AddCSVFeatures(string csv)
    {
        string[] tags = SplitCSVString(csv);

        foreach (string tag in tags)
        {
            AddFeature(tag);
        }
    }

    public void AddCSVTags(string csv, string prefix)
    {
        char[] trimChars = { ' ' };
        string[] tags = SplitCSVString(csv);

        foreach (string tag in tags)
        {
            string tagString = $"{prefix} {tag}";
            AddTag(tagString.Trim(trimChars));
        }
    }

    public void AddCSVTags(string csv)
    {
        string[] tags = SplitCSVString(csv);

        foreach (string tag in tags)
        {
            AddTag(tag);
        }
    }

    public ReleaseDate? WindowsReleaseDate()
    {
        ReleaseDate? date;

        if (this.ReleaseDates.TryGetValue("Windows", out date))
        {
            return date;
        }
        else
        {
            return null;
        }
    }

    public void AddReleaseDate(string platform, DateTime? date)
    {
        this.ReleaseDates[platform] = new ReleaseDate((DateTime)date);
    }

    public string[] SplitCSVString(string csv)
    {
        TextFieldParser parser = new TextFieldParser(new StringReader(csv));
        parser.SetDelimiters(",");
        return parser.ReadFields();
    }

    public void AddGenres(string genreCsv)
    {
        string[] genres = SplitCSVString(genreCsv);

        foreach (string genre in genres)
        {
            this.Genres.Add(new MetadataNameProperty(genre));
        }
    }

    public void SetXboxPlayAnywhere()
    {
        if (BuiltinExtensions.GetExtensionFromId(this.LibraryGame.PluginId) == BuiltinExtension.XboxLibrary)
        {
            this.AddFeature("Xbox Play Anywhere");
        }
    }

    public void AddVRFeature()
    {
        this.AddFeature("VR");
    }
}
