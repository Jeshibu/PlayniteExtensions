﻿using GamesSizeCalculator.PS3;
using GamesSizeCalculator.SteamSizeCalculation;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace GamesSizeCalculator;

public class GamesSizeCalculator : MetadataPlugin
{
    private static readonly ILogger logger = LogManager.GetLogger();

    private GamesSizeCalculatorSettingsViewModel settings { get; set; }

    public override Guid Id { get; } = Guid.Parse("97cc59db-3f80-4852-8bfc-a80304f9efe9");

    public override string Name { get; } = "Games Size Calculator";

    public override List<MetadataField> SupportedFields { get; } = [MetadataField.InstallSize];

    public GamesSizeCalculator(IPlayniteAPI api) : base(api)
    {
        settings = new GamesSizeCalculatorSettingsViewModel(this);
        Properties = new MetadataPluginProperties
        {
            HasSettings = true
        };
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        sizeCalculators.Clear();
        return new GamesSizeCalculatorSettingsView();
    }

    private ISteamAppIdUtility GetDefaultSteamAppUtility()
    {
        var appListCache = new CachedFileDownloader("https://api.steampowered.com/ISteamApps/GetAppList/v2/",
                Path.Combine(GetPluginUserDataPath(), "SteamAppList.json"),
                TimeSpan.FromHours(18),
                Encoding.UTF8);

        return new SteamAppIdUtility(appListCache);
    }

    private List<ISizeCalculator> sizeCalculators { get; } = [];
    private SteamApiClient steamApiClient;

    private SteamApiClient SteamApiClient
    {
        get
        {
            if (steamApiClient != null)
            {
                var timeSinceLastUse = DateTime.Now - steamApiClient.LastUsed;
                if (timeSinceLastUse.TotalSeconds > 60)
                {
                    try
                    {
                        steamApiClient.Dispose();
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Error disposing of SteamApiClient");
                    }
                    steamApiClient = null;
                }
            }

            return steamApiClient ??= new();
        }
    }

    private IEnumerable<ISizeCalculator> GetSizeCalculators()
    {
        if (settings.Settings.GetUninstalledGameSizeFromSteam)
            yield return new SteamSizeCalculator(SteamApiClient, GetDefaultSteamAppUtility(), settings.Settings);
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        return new InstallSizeProvider(options.GameData, PlayniteApi, GetSizeCalculators().ToList());
    }

    public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
    {
        if (args.Games.Any(PS3InstallSizeCalculator.IsPs3Rom))
            yield return new() { Description = PlayniteApi.Resources.GetString("LOCGame_Sizes_Calculator_PS3_Option"), Action = FixPs3RomInstallSize };
    }

    private void FixPs3RomInstallSize(GameMenuItemActionArgs args)
    {
        using (PlayniteApi.Database.BufferedUpdate())
        PlayniteApi.Dialogs.ActivateGlobalProgress((GlobalProgressActionArgs a) =>
        {
            a.ProgressMaxValue = args.Games.Count;

            var ps3Calc = new PS3InstallSizeCalculator(PlayniteApi);
            foreach (var g in args.Games)
            {
                if (ps3Calc.IsPreferredInstallSizeCalculator(g))
                {
                    var installSize = ps3Calc.GetInstallSize(g);
                    if (installSize > 0 && g.InstallSize != installSize)
                    {
                        g.InstallSize = installSize;
                        g.Modified = DateTime.Now;
                        PlayniteApi.Database.Games.Update(g);
                    }
                }
                a.CurrentProgressValue++;
            }
        }, new GlobalProgressOptions(PlayniteApi.Resources.GetString("LOCGame_Sizes_Calculator_PS3_Progress"), cancelable: true) { IsIndeterminate = false });
    }

    public override void Dispose()
    {
        steamApiClient?.Dispose();
        base.Dispose();
    }
}