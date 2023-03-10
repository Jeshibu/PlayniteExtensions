using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LaunchBoxMetadata
{
    public class LaunchBoxMetadataSettings : ObservableObject
    {
        public bool UseLaunchBoxLink { get; set; } = true;
        public bool UseWikipediaLink { get; set; } = true;
        public bool UseVideoLink { get; set; } = true;
        public string MetadataZipEtag { get; set; }
        public DateTimeOffset? MetadataZipLastModified { get; set; }
        public LaunchBoxImageSourceSettings Cover { get; set; }
        public LaunchBoxImageSourceSettings Background { get; set; }

        public LaunchBoxMetadataSettings()
        {
            Cover = new LaunchBoxImageSourceSettings
            {
                AspectRatio = AspectRatio.Vertical,
                MaxHeight = 900,
                MaxWidth = 600,
                MinHeight = 300,
                MinWidth = 200,
            };
            Background = new LaunchBoxImageSourceSettings
            {
                AspectRatio = AspectRatio.Horizontal,
                MaxHeight = 1440,
                MaxWidth = 2560,
                MinHeight = 500,
                MinWidth = 1000,
            };
        }
    }

    public class LaunchBoxImageSourceSettings
    {
        public ObservableCollection<CheckboxSetting> ImageTypes { get; set; } = new ObservableCollection<CheckboxSetting>();
        public int MaxHeight { get; set; }
        public int MaxWidth { get; set; }
        public int MinHeight { get; set; }
        public int MinWidth { get; set; }
        public AspectRatio AspectRatio { get; set; }
    }

    public class CheckboxSetting
    {
        public bool Checked { get; set; }
        public string Name { get; set; }

        public override string ToString()
        {
            var symbol = Checked ? "✔" : "❌";
            return $"{symbol} {Name}";
        }
    }

    public enum AspectRatio
    {
        Any,
        Vertical,
        Horizontal,
        Square,
    }

    public class LaunchBoxMetadataSettingsViewModel : PluginSettingsViewModel<LaunchBoxMetadataSettings, LaunchBoxMetadata>
    {
        public LaunchBoxMetadataSettingsViewModel(LaunchBoxMetadata plugin) : base(plugin, plugin.PlayniteApi)
        {
            Settings = LoadSavedSettings() ?? new LaunchBoxMetadataSettings();
            InitializeImageTypeLists();
        }

        private RelayCommand initializeDatabaseCommand;
        private RelayCommand downloadMetadataCommand;

        public ICommand InitializeDatabaseCommand
        {
            get
            {
                return initializeDatabaseCommand ?? (initializeDatabaseCommand = new RelayCommand(InitializeDatabase));
            }
        }

        public ICommand DownloadMetadataCommand
        {
            get
            {
                return downloadMetadataCommand ?? (downloadMetadataCommand = new RelayCommand(DownloadMetadata));
            }
        }

        private LaunchBoxDatabase GetDatabase()
        {
            return new LaunchBoxDatabase(Plugin.GetPluginUserDataPath());
        }

        private void InitializeImageTypeLists()
        {
            try
            {
                var database = GetDatabase();
                var types = database.GetGameImageTypes().ToList();
                InitializeImageTypeList(types, Settings.Cover, "Box - Front", "Box - Front - Reconstructed", "Fanart - Box - Front");
                InitializeImageTypeList(types, Settings.Background, "Fanart - Background", "Screenshot - Gameplay", "Screenshot - Game Title", "Banner");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error initializing image types");
            }
        }

        private void InitializeImageTypeList(List<string> types, LaunchBoxImageSourceSettings imgSettings, params string[] defaultChecked)
        {
            if (imgSettings.ImageTypes.Count == 0) //put default selected items at the top
            {
                foreach (var def in defaultChecked)
                {
                    imgSettings.ImageTypes.Add(new CheckboxSetting { Name = def, Checked = true });
                }
            }

            foreach (var t in types)
            {
                if (!imgSettings.ImageTypes.Any(x => x.Name == t))
                    imgSettings.ImageTypes.Add(new CheckboxSetting { Name = t, Checked = defaultChecked.Contains(t) });
            }
        }

        private void DownloadMetadata()
        {
            MetadataZipFileHandler zipfileHandler = null;
            try
            {
                zipfileHandler = new MetadataZipFileHandler(PlayniteApi, Settings);

                var zipFilePath = zipfileHandler.DownloadMetadataZipFile();
                if (zipFilePath == null || !File.Exists(zipFilePath))
                    return;

                var xmlPath = zipfileHandler.ExtractMetadataXmlFromZipFile(zipFilePath);
                if (xmlPath == null || !File.Exists(xmlPath))
                    return;

                var xmlParser = new LaunchBoxXmlParser(xmlPath);
                var database = GetDatabase();
                PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
                {
                    database.CreateDatabase(xmlParser, a);
                }, new GlobalProgressOptions("Initializing database...", false));


                OnPropertyChanged(nameof(StatusText));
                InitializeImageTypeLists();
                PlayniteApi.Dialogs.ShowMessage("Local LaunchBox metadata database successfully initialized!", "LaunchBox database", System.Windows.MessageBoxButton.OK);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Error while downloading metadata zip and initializing database");
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message);
            }
            finally
            {
                zipfileHandler?.Dispose();
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

                OnPropertyChanged(nameof(StatusText));
                InitializeImageTypeLists();
            }
            catch (Exception ex)
            {
                PlayniteApi.Dialogs.ShowErrorMessage(ex.Message);
            }
        }

        public string StatusText
        {
            get
            {
                var databaseFilePath = LaunchBoxDatabase.GetFilePath(Plugin.GetPluginUserDataPath());
                if (!File.Exists(databaseFilePath))
                    return "Local database not initialized";

                var lastWrite = File.GetLastWriteTime(databaseFilePath);
                return $"Database last updated {lastWrite:g}";
            }
        }

        public List<AspectRatio> AspectRatios { get; } = new List<AspectRatio> { AspectRatio.Any, AspectRatio.Vertical, AspectRatio.Horizontal, AspectRatio.Square };
    }
}