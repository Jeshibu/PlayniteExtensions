using MutualGames.Clients;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MutualGames.Models.Settings
{
    public class MutualGamesSettingsViewModel : PluginSettingsViewModel<MutualGamesSettings, MutualGames>
    {
        private List<GameFeature> gameFeatures;
        private IFriendsGamesClient[] clients;
        public IFriendsGamesClient[] Clients => clients ?? (clients = Plugin.GetClients().ToArray());
        public GameField[] ImportFieldOptions { get; } = new[] { GameField.Categories, GameField.Tags };
        public List<GameFeature> Features
        {
            get
            {
                if (gameFeatures != null)
                    return gameFeatures;

                var output = new List<GameFeature> { new GameFeature { Id = Guid.Empty, Name = "None" } };
                output.AddRange(PlayniteApi.Database.Features.OrderBy(f => f.Name));
                return gameFeatures = output;
            }
        }

        public bool SameLibraryChecked
        {
            get => Settings.CrossLibraryImportMode == CrossLibraryImportMode.SameLibraryOnly;
            set { if (value) SetCrossLibraryImportMode(CrossLibraryImportMode.SameLibraryOnly); }
        }
        public bool ImportAllChecked
        {
            get => Settings.CrossLibraryImportMode == CrossLibraryImportMode.ImportAll;
            set { if (value) SetCrossLibraryImportMode(CrossLibraryImportMode.ImportAll); }
        }
        public bool ImportAllWithFeatureChecked
        {
            get => Settings.CrossLibraryImportMode == CrossLibraryImportMode.ImportAllWithFeature;
            set { if (value) SetCrossLibraryImportMode(CrossLibraryImportMode.ImportAllWithFeature); }
        }

        private void SetCrossLibraryImportMode(CrossLibraryImportMode crossLibraryImportMode)
        {
            Settings.CrossLibraryImportMode = crossLibraryImportMode;
            base.OnPropertyChanged(nameof(SameLibraryChecked));
            base.OnPropertyChanged(nameof(ImportAllChecked));
            base.OnPropertyChanged(nameof(ImportAllWithFeatureChecked));
        }

        public MutualGamesSettingsViewModel(MutualGames plugin) : base(plugin, plugin.PlayniteApi)
        {
            InitializeSettings();
        }

        public void InitializeSettings()
        {
            Settings = Plugin.LoadPluginSettings<MutualGamesSettings>();

            var firstRun = Settings == null;

            if (firstRun)
            {
                Settings = new MutualGamesSettings();
            }

            if (Settings.ImportCrossLibraryFeatureId == default)
            {
                var titleComparer = new TitleComparer();
                var crossPlatformFeature = Features.FirstOrDefault(f => titleComparer.Equals(f.Name, "Cross-Platform Multiplayer"));
                if (crossPlatformFeature != null)
                {
                    Settings.ImportCrossLibraryFeatureId = crossPlatformFeature.Id;

                    if (firstRun)
                        Settings.CrossLibraryImportMode = CrossLibraryImportMode.ImportAllWithFeature;
                }
            }

            foreach (var c in Clients)
            {
                var existingSettings = Settings.FriendSources.FirstOrDefault(fs => fs.Source == c.Source);
                if (existingSettings == null)
                {
                    Settings.FriendSources.Add(new FriendSourceSettings { Client = c, Name = c.Name, Source = c.Source, PlayniteApi = PlayniteApi });
                }
                else
                {
                    existingSettings.Client = c;
                    existingSettings.PlayniteApi = PlayniteApi;
                }
            }
        }

        public override bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            try
            {
                var exampleFriend = new FriendAccountInfo { Id = "42069", Name = "xXx_1337Slayer_xXx", Source = FriendSource.Steam };
                var formatted = string.Format(Settings.PropertyNameFormat, exampleFriend.Name, exampleFriend.Source);
            }
            catch (ArgumentNullException)
            {
                errors.Add("Format can't be empty");
            }
            catch (FormatException)
            {
                errors.Add("Format is invalid. Make sure you only use {0} and {1} as variables, and escape any other uses of { or } with the same character (so {{ or }})");
            }

            if (Settings.CrossLibraryImportMode == CrossLibraryImportMode.ImportAllWithFeature && Settings.ImportCrossLibraryFeatureId == default)
                errors.Add("Please select a feature for the cross-library import mode");

            return errors.Count == 0;
        }
    }
}