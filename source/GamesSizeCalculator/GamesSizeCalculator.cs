using GamesSizeCalculator.SteamSizeCalculation;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteUtilitiesCommon;
using PluginsCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace GamesSizeCalculator
{
    public class GamesSizeCalculator : MetadataPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GamesSizeCalculatorSettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("97cc59db-3f80-4852-8bfc-a80304f9efe9");

        public override string Name { get; } = "Games Size Calculator";

        public override List<MetadataField> SupportedFields { get; } = new List<MetadataField> { MetadataField.InstallSize };

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
            onlineSizeCalculators.Clear();
            return new GamesSizeCalculatorSettingsView();
        }

        private ISteamAppIdUtility GetDefaultSteamAppUtility()
        {
            var appListCache = new CachedFileDownloader("https://api.steampowered.com/ISteamApps/GetAppList/v2/",
                    Path.Combine(GetPluginUserDataPath(), "SteamAppList.json"),
                    TimeSpan.FromDays(3),
                    Encoding.UTF8);

            return new SteamAppIdUtility(appListCache);
        }

        private List<IOnlineSizeCalculator> onlineSizeCalculators { get; } = new List<IOnlineSizeCalculator>();
        private SteamApiClient steamApiClient;

        private SteamApiClient GetSteamApiClient()
        {
            return steamApiClient ?? (steamApiClient = new SteamApiClient());
        }

        private ICollection<IOnlineSizeCalculator> GetOnlineSizeCalculators()
        {
            if (onlineSizeCalculators.Any())
                return onlineSizeCalculators;

            if (settings.Settings.GetUninstalledGameSizeFromSteam)
            {
                onlineSizeCalculators.Add(new SteamSizeCalculator(GetSteamApiClient(), GetDefaultSteamAppUtility(), settings.Settings));
            }
            if (settings.Settings.GetUninstalledGameSizeFromGog)
            {
                onlineSizeCalculators.Add(new GOG.GogSizeCalculator(new GOG.HttpDownloaderWrapper(), settings.Settings.GetSizeFromGogNonGogGames));
            }
            return onlineSizeCalculators;
        }

        public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
        {
            return new OnlineInstallSizeProvider(options.GameData, PlayniteApi, GetOnlineSizeCalculators());
        }

        public override void Dispose()
        {
            steamApiClient?.Dispose();
            base.Dispose();
        }
    }

    public class OnlineInstallSizeProvider : OnDemandMetadataProvider
    {
        public OnlineInstallSizeProvider(Game game, IPlayniteAPI playniteAPI, ICollection<IOnlineSizeCalculator> onlineSizeCalculators)
        {
            Game = game;
            PlayniteApi = playniteAPI;
            OnlineSizeCalculators = onlineSizeCalculators;
        }

        public override List<MetadataField> AvailableFields { get; } = new List<MetadataField> { MetadataField.InstallSize };
        public Game Game { get; }
        public IPlayniteAPI PlayniteApi { get; }
        public ICollection<IOnlineSizeCalculator> OnlineSizeCalculators { get; }
        private readonly ILogger logger = LogManager.GetLogger();

        public override ulong? GetInstallSize(GetMetadataFieldArgs args)
        {
            ulong size = GetInstallSizeOnline();
            return size == 0 ? (ulong?)null : size;
        }

        private ulong GetInstallSizeOnline(IOnlineSizeCalculator sizeCalculator)
        {
            try
            {
                var sizeTask = sizeCalculator.GetInstallSizeAsync(Game);
                if (sizeTask.Wait(7000))
                {
                    return sizeTask.Result ?? 0L;
                }
                else
                {
                    logger.Warn($"Timed out while getting online {sizeCalculator.ServiceName} install size for {Game.Name}");
                    return 0L;
                }
            }
            catch (Exception e)
            {
                logger.Error(e, $"Error while getting file size online from {sizeCalculator?.ServiceName} for {Game?.Name}");
                PlayniteApi.Notifications.Messages.Add(
                    new NotificationMessage("GetOnlineSizeError" + Game.Id.ToString(),
                        string.Format(ResourceProvider.GetString("LOCGame_Sizes_Calculator_NotificationMessageErrorGetOnlineSize"), sizeCalculator.ServiceName, Game.Name, e.Message),
                        NotificationType.Error));
                return 0;
            }
        }

        private ulong GetInstallSizeOnline()
        {
            if (!(OnlineSizeCalculators?.Count > 0 && PlayniteUtilities.IsGamePcGame(Game)))
            {
                return 0;
            }

            ulong size = 0;

            var alreadyRan = new List<IOnlineSizeCalculator>();
            //check the preferred online size calculators first (Steam for Steam games, GOG for GOG games, etc)
            foreach (var sizeCalculator in OnlineSizeCalculators)
            {
                if (!sizeCalculator.IsPreferredInstallSizeCalculator(Game))
                {
                    continue;
                }

                size = GetInstallSizeOnline(sizeCalculator);
                alreadyRan.Add(sizeCalculator);
                if (size != 0)
                {
                    break;
                }
            }

            //go through every online size calculator as a fallback
            if (size == 0)
            {
                foreach (var sizeCalculator in OnlineSizeCalculators)
                {
                    if (alreadyRan.Contains(sizeCalculator))
                    {
                        continue;
                    }

                    size = GetInstallSizeOnline(sizeCalculator);
                    if (size != 0)
                    {
                        break;
                    }
                }
            }

            return size;
        }

    }
}