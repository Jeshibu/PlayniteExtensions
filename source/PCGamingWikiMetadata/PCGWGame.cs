using System;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;
using System.IO;

namespace PCGamingWikiMetadata
{
    public class PCGWGame : GenericItemOption
    {
        private readonly ILogger logger = LogManager.GetLogger();
        public int PageID { get; set; }

        private PCGamingWikiMetadataSettings settings;

        private List<MetadataProperty> genres;
        public List<MetadataProperty> Genres { get { return genres; } }
        private List<MetadataProperty> developers;
        public List<MetadataProperty> Developers { get { return developers; } }
        private List<MetadataProperty> publishers;
        public List<MetadataProperty> Publishers { get { return publishers; } }
        private List<MetadataProperty> features;
        public List<MetadataProperty> Features { get { return features; } }
        private List<MetadataProperty> series;
        public List<MetadataProperty> Series { get { return series; } }
        private List<Link> links;
        public List<Link> Links { get { return links; } }
        private List<MetadataProperty> tags;
        public List<MetadataProperty> Tags { get { return tags; } }

        private IDictionary<string, int?> reception;
        private IDictionary<string, ReleaseDate?> ReleaseDates;

        public Game LibraryGame;

        public PCGWGame(PCGamingWikiMetadataSettings settings)
        {
            this.settings = settings;
            this.links = new List<Link>();
            this.genres = new List<MetadataProperty>();
            this.features = new List<MetadataProperty>();
            this.series = new List<MetadataProperty>();
            this.developers = new List<MetadataProperty>();
            this.publishers = new List<MetadataProperty>();
            this.tags = new List<MetadataProperty>();
            this.ReleaseDates = new Dictionary<string, ReleaseDate?>();
            this.reception = new Dictionary<string, int?>();
        }

        public PCGWGame(PCGamingWikiMetadataSettings settings, string name, int pageid) : this(settings)
        {
            this.Name = name;
            this.PageID = pageid;
            AddPCGamingWikiLink();
        }

        protected Link PCGamingWikiLink()
        {
            string escapedName = Uri.EscapeUriString(this.Name);
            return new Link("PCGamingWiki", $"https://www.pcgamingwiki.com/wiki/{escapedName}");
        }

        public void AddPCGamingWikiLink()
        {
            this.links.Add(PCGamingWikiLink());
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
            this.tags.Add(new MetadataNameProperty(t));
        }

        public void AddFeature(string t)
        {
            this.features.AddMissing(new MetadataNameProperty(t));
        }

        public void AddSeries(string t)
        {
            this.series.Add(new MetadataNameProperty(t));
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
                this.genres.Add(new MetadataNameProperty(genre));
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
}
