using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LaunchBoxMetadata
{
    public class LaunchBoxMetadataSettings : ObservableObject
    {
        public bool UseLaunchBoxLink { get; set; }
        public bool UseWikipediaLink { get; set; }
        public bool UseVideoLink { get; set; }
    }

    public class LaunchBoxMetadataSettingsViewModel : PluginSettingsViewModel<LaunchBoxMetadataSettings, LaunchBoxMetadata>
    {
        public LaunchBoxMetadataSettingsViewModel(LaunchBoxMetadata plugin) : base(plugin, plugin.PlayniteApi)
        {
            Settings = LoadSavedSettings() ?? new LaunchBoxMetadataSettings();
        }

        private RelayCommand initializeDatabaseCommand;

        public ICommand InitializeDatabaseCommand
        {
            get
            {
                if (initializeDatabaseCommand == null)
                {
                    initializeDatabaseCommand = new RelayCommand(InitializeDatabase);
                }

                return initializeDatabaseCommand;
            }
        }

        private void InitializeDatabase()
        {
            try
            {
                var xmlPath = PlayniteApi.Dialogs.SelectFile("Metadata.xml|Metadata.xml");
                if (xmlPath == null || !File.Exists(xmlPath))
                    return;

                var xmlParser = new LaunchBoxXmlParser(xmlPath);
                var database = new LaunchBoxDatabase(Plugin.GetPluginUserDataPath());
                PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
                {
                    database.CreateDatabase(xmlParser);
                }, new GlobalProgressOptions("Initializing database...", false));

                PlayniteApi.Dialogs.ShowMessage("LaunchBox metadata database successfully initialized!", "LaunchBox database", System.Windows.MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message);
            }
            OnPropertyChanged(nameof(StatusText));
        }

        public string StatusText
        {
            get
            {
                var databaseFilePath = LaunchBoxDatabase.GetFilePath(Plugin.GetPluginUserDataPath());
                if(!File.Exists(databaseFilePath))
                    return "Local database not initialized";

                var lastWrite = File.GetLastWriteTime(databaseFilePath);
                return $"Database last updated {lastWrite:g}";
            }
        }
    }
}