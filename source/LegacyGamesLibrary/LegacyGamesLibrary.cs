using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LegacyGamesLibrary
{
    public class LegacyGamesLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private LegacyGamesLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("34c3178f-6e1d-4e27-8885-99d4f031b168");

        // Change to something more appropriate
        public override string Name => "Legacy Games";

        // Implementing Client adds ability to open it via special menu in playnite.
        //public override LibraryClient Client { get; } = new LegacyGamesLibraryClient();

        private AggregateMetadataGatherer MetadataGatherer { get; }

        public LegacyGamesLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new LegacyGamesLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = false,
                CanShutdownClient = false,
                HasCustomizedGameImport = false,
            };
            MetadataGatherer = new AggregateMetadataGatherer(new LegacyGamesRegistryReader(new RegistryValueProvider()), new AppStateReader(), api);
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            return MetadataGatherer.GetGames(args.CancelToken);
        }

        public override LibraryMetadataProvider GetMetadataDownloader()
        {
            return MetadataGatherer;
        }

        //public override ISettings GetSettings(bool firstRunSettings)
        //{
        //    return settings;
        //}

        //public override UserControl GetSettingsView(bool firstRunSettings)
        //{
        //    return new LegacyGamesLibrarySettingsView();
        //}
    }
}