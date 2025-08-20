﻿using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace LaunchBoxMetadata;

public class LaunchBoxMetadataSettings : ObservableObject
{
    public bool UseLaunchBoxLink { get; set; } = true;
    public bool UseWikipediaLink { get; set; } = true;
    public bool UseVideoLink { get; set; } = true;
    public string MetadataZipEtag { get; set; }
    public DateTimeOffset? MetadataZipLastModified { get; set; }
    public LaunchBoxImageSourceSettings Icon { get; set; }
    public LaunchBoxImageSourceSettings Cover { get; set; }
    public LaunchBoxImageSourceSettings Background { get; set; }
    public ObservableCollection<RegionSetting> Regions { get; set; } = [];
    public bool PreferGameRegion { get; set; } = true;

    public LaunchBoxMetadataSettings()
    {
        Icon = new LaunchBoxImageSourceSettings
        {
            AspectRatio = AspectRatio.AnyExtendToSquare,
            MaxHeight = 256,
            MaxWidth = 256,
            MinHeight = 32,
            MinWidth = 32,
        };
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
    public int AdviseDatabaseUpdateAfterDays { get; set; } = 30;

    public int DatabaseVersion { get; set; } = 1;

    public const int CurrentDatabaseVersion = 2;
}

public class LaunchBoxImageSourceSettings
{
    public ObservableCollection<CheckboxSetting> ImageTypes { get; set; } = [];
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

public class RegionSetting : CheckboxSetting
{
    public string Aliases { get; set; }
}

public enum AspectRatio
{
    Any,
    Vertical,
    Horizontal,
    Square,
    AnyExtendToSquare,
}

public class LaunchBoxMetadataSettingsViewModel : PluginSettingsViewModel<LaunchBoxMetadataSettings, LaunchBoxMetadata>
{
    public LaunchBoxMetadataSettingsViewModel(LaunchBoxMetadata plugin) : base(plugin, plugin.PlayniteApi)
    {
        Settings = LoadSavedSettings() ?? new LaunchBoxMetadataSettings() {  DatabaseVersion = LaunchBoxMetadataSettings.CurrentDatabaseVersion };
        InitializeDatabaseLists();
    }

    private RelayCommand initializeDatabaseCommand;
    private RelayCommand downloadMetadataCommand;

    public ICommand InitializeDatabaseCommand
    {
        get
        {
            return initializeDatabaseCommand ??= new RelayCommand(InitializeDatabase);
        }
    }

    public ICommand DownloadMetadataCommand
    {
        get
        {
            return downloadMetadataCommand ??= new RelayCommand(DownloadMetadata);
        }
    }

    private LaunchBoxDatabase GetDatabase()
    {
        return new LaunchBoxDatabase(Plugin.GetPluginUserDataPath());
    }

    private void InitializeDatabaseLists()
    {
        try
        {
            var database = GetDatabase();
            var types = database.GetGameImageTypes().ToList();
            InitializeImageTypeList(types, Settings.Icon, "Icon", "Clear Logo");
            InitializeImageTypeList(types, Settings.Cover, "Box - Front", "Box - Front - Reconstructed", "Fanart - Box - Front");
            InitializeImageTypeList(types, Settings.Background, "Fanart - Background", "Screenshot - Gameplay", "Screenshot - Game Title", "Banner");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error initializing database lists");
        }

        try
        {
            var database = GetDatabase();
            InitializeRegionList(database.GetRegions().ToList());

        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Error initializing database lists");
            InitializeRegionList([null]);
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

    private void InitializeRegionList(List<string> regions)
    {
        foreach (var regionName in regions)
        {
            if (!Settings.Regions.Any(r => r.Name == regionName))
            {
                Settings.Regions.Add(new RegionSetting { Checked = true, Name = regionName, Aliases = GetDefaultRegionAliases(regionName) });
            }
        }
    }

    private string GetDefaultRegionAliases(string region)
    {
        return region switch
        {
            "United States" => "US, USA",
            "United Kingdom" => "UK, GB, Great Britain",
            "Japan" => "JP, JA",
            _ => null,
        };
    }

    private void DownloadMetadata()
    {
        MetadataZipFileHandler zipfileHandler = null;
        try
        {
            zipfileHandler = new MetadataZipFileHandler(PlayniteApi, Settings);

            var zipFilePath = zipfileHandler.DownloadMetadataZipFile(warnOnSameVersion: Settings.DatabaseVersion == LaunchBoxMetadataSettings.CurrentDatabaseVersion);
            if (zipFilePath == null || !File.Exists(zipFilePath))
                return;

            var xmlPath = zipfileHandler.ExtractMetadataXmlFromZipFile(zipFilePath);
            if (xmlPath == null || !File.Exists(xmlPath))
                return;

            var xmlParser = new LaunchBoxXmlParser(xmlPath);
            var database = GetDatabase();
            Exception exception = null;
            PlayniteApi.Dialogs.ActivateGlobalProgress(a =>
            {
                try
                {
                    database.CreateDatabase(xmlParser, a);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Error creating database");
                    exception = ex;
                }
            }, new GlobalProgressOptions("Initializing database...", false));

            OnPropertyChanged(nameof(StatusText));

            if (exception != null)
            {
                PlayniteApi.Dialogs.ShowMessage($"Failed to initialize local Launchbox metadata database: {exception.Message}", "LaunchBox database", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            else
            {
                InitializeDatabaseLists();
                Settings.DatabaseVersion = LaunchBoxMetadataSettings.CurrentDatabaseVersion;
                base.Plugin.SavePluginSettings(Settings);
                PlayniteApi.Dialogs.ShowMessage("Local LaunchBox metadata database successfully initialized!", "LaunchBox database", System.Windows.MessageBoxButton.OK);
            }
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
                database.CreateDatabase(xmlParser, a);
            }, new GlobalProgressOptions("Initializing database...", false));

            PlayniteApi.Dialogs.ShowMessage("LaunchBox metadata database successfully initialized!", "LaunchBox database", System.Windows.MessageBoxButton.OK);

            OnPropertyChanged(nameof(StatusText));
            InitializeDatabaseLists();
            Settings.DatabaseVersion = LaunchBoxMetadataSettings.CurrentDatabaseVersion;
            base.Plugin.SavePluginSettings(Settings);
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

    public Dictionary<AspectRatio, string> AspectRatios { get; } = new()
    {
        { AspectRatio.Any, "Any" },
        { AspectRatio.Vertical, "Vertical" },
        { AspectRatio.Horizontal, "Horizontal" },
        { AspectRatio.Square, "Square" },
        { AspectRatio.AnyExtendToSquare, "Any (extend to square)" },
    };
}