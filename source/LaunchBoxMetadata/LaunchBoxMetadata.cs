using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LaunchBoxMetadata
{
    public class LaunchBoxMetadata : MetadataPlugin
    {
        //So for anyone using GongSolutions.Wpf.DragDrop - be aware you have to instantiate something from it before referencing the package in your XAML
        private GongSolutions.Wpf.DragDrop.DefaultDragHandler dropInfo = new GongSolutions.Wpf.DragDrop.DefaultDragHandler();
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlatformUtility platformUtility;

        private LaunchBoxMetadataSettingsViewModel settings { get; set; }
        private LaunchBoxWebscraper webscraper { get; set; }

        public override Guid Id { get; } = Guid.Parse("3b1908f2-de02-48c9-9633-10d978903652");

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Description,
            MetadataField.Platform,
            MetadataField.CommunityScore,
            MetadataField.ReleaseDate,
            MetadataField.AgeRating,
            MetadataField.Genres,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.CoverImage,
            MetadataField.BackgroundImage,
            MetadataField.Links,
        };

        public override string Name => "LaunchBox";

        public LaunchBoxMetadata(IPlayniteAPI api) : base(api)
        {
            settings = new LaunchBoxMetadataSettingsViewModel(this);
            Properties = new MetadataPluginProperties
            {
                HasSettings = true
            };
            platformUtility = new PlatformUtility(PlayniteApi);
            webscraper = new LaunchBoxWebscraper(new WebDownloader());
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            var userDataPath = GetPluginUserDataPath();
            var dbPath = LaunchBoxDatabase.GetFilePath(userDataPath);
            if (!File.Exists(dbPath))
            {
                PlayniteApi.Notifications.Add(new NotificationMessage("launchbox-database-missing", "LaunchBox database not initialized. Click here to initialize it.", NotificationType.Error, () => OpenSettingsView()));
                return null;
            }
            return new LaunchBoxMetadataProvider(options, this, settings.Settings, new LaunchBoxDatabase(userDataPath), platformUtility, webscraper);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new LaunchBoxMetadataSettingsView();
        }
    }
}