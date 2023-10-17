using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Windows.Controls;
using XboxMetadata.Scrapers;

namespace XboxMetadata
{
    public class XboxMetadata : MetadataPlugin
    {
        //So for anyone using GongSolutions.Wpf.DragDrop - be aware you have to instantiate something from it before referencing the package in your XAML
        private GongSolutions.Wpf.DragDrop.DefaultDragHandler dropInfo = new GongSolutions.Wpf.DragDrop.DefaultDragHandler();
        private static readonly ILogger logger = LogManager.GetLogger();
        private readonly IWebDownloader downloader = new WebDownloader();
        private readonly IPlatformUtility platformUtility;
        private readonly ScraperManager scraperManager;

        private XboxMetadataSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("b7d106c0-e4f3-4344-911f-46c195419e6a");

        public override List<MetadataField> SupportedFields { get; } = XboxMetadataProvider.Fields;

        public override string Name => "Xbox Store";

        public XboxMetadata(IPlayniteAPI api) : base(api)
        {
            settings = new XboxMetadataSettingsViewModel(this);
            platformUtility = new PlatformUtility(PlayniteApi);
            scraperManager = new ScraperManager(downloader, platformUtility);
            Properties = new MetadataPluginProperties { HasSettings = true };
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new XboxMetadataProvider(options, settings.Settings, PlayniteApi, scraperManager);
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new XboxMetadataSettingsView();
        }
    }
}