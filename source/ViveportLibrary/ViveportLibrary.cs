using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace ViveportLibrary
{
    public class ViveportLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private ViveportLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("97d85dbd-ad52-4834-bf4b-f6681f1445cc");

        public override string Name { get; } = "Viveport";

        public override LibraryClient Client { get; } = new ViveportLibraryClient();

        private IAppDataReader AppDataReader { get; }

        public ViveportLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new ViveportLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                HasSettings = true
            };
            AppDataReader = new AppDataReader();
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            var installedApps = AppDataReader.GetInstalledApps();
            if (installedApps == null)
            {
                PlayniteApi.Notifications.Add(new NotificationMessage("viveport-installed-app-reader-error", "Couldn't read the locally installed Viveport apps. Check your Viveport desktop client installation.", NotificationType.Error));
                yield break;
            }

            var licensedApps = AppDataReader.GetLicensedApps();
            if (licensedApps == null)
            {
                PlayniteApi.Notifications.Add(new NotificationMessage("viveport-installed-app-reader-error", "Couldn't read the Viveport licensed apps. Check your Viveport desktop client installation.", NotificationType.Error));
                yield break;
            }

            var installedDict = installedApps.ToDictionary(x => x.AppId);
            var licensedDict = licensedApps.ToDictionary(x => x.Id);

            var keys = licensedDict.Keys.ToHashSet();
            foreach (var key in installedDict.Keys)
                keys.Add(key);

            foreach (var key in keys)
            {
                installedDict.TryGetValue(key, out var installedAppData);

                if (!licensedDict.TryGetValue(key, out var licensedAppData))
                    logger.Warn($"Couldn't find license data for app {key} ({installedAppData?.Title})");

                var game = new GameMetadata
                {
                    GameId = key,
                    Name = licensedAppData.Title ?? installedAppData.Title,
                    Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") },
                    InstallDirectory = installedAppData?.Path,
                    IsInstalled = installedAppData != null,
                    Source = new MetadataNameProperty("Viveport"),
                };

                if (settings.Settings.UseCovers)
                {
                    string coverUrl = installedAppData?.ImageUri;
                    if (coverUrl == null && licensedAppData != null && licensedAppData.Thumbnails.TryGetValue("medium", out var thumbnail))
                    {
                        coverUrl = thumbnail.Url;
                    }

                    if (!string.IsNullOrWhiteSpace(coverUrl))
                        game.CoverImage = new MetadataFile(coverUrl);
                }

                if (licensedAppData != null && settings.Settings.ImportHeadsetsAsPlatforms)
                {
                    foreach (var platform in licensedAppData.SupportedDeviceList)
                    {
                        if (platform == "VStreaming")
                            continue; //what the hell is VStreaming anyway

                        game.Platforms.Add(new MetadataNameProperty(SplitPascalCase(platform)));
                    }
                }

                yield return game;
            }
        }

        public static string SplitPascalCase(string pascalCaseStr)
        {
            StringBuilder output = new StringBuilder();
            for (int i = 0; i < pascalCaseStr.Length; i++)
            {
                char a = pascalCaseStr[i];

                if (i + 1 < pascalCaseStr.Length)
                {
                    char b = pascalCaseStr[i + 1];
                    if (char.IsLower(a) && char.IsUpper(b))
                    {
                        output.Append(a);
                        output.Append(' ');
                        output.Append(b);
                        i++;
                        continue;
                    }
                    if (i + 3 < pascalCaseStr.Length)
                    {
                        char c = pascalCaseStr[i + 2];
                        char d = pascalCaseStr[i + 3];
                        if (a == 'H' && b == 't' && c == 'c' && char.IsUpper(d))
                        {
                            output.Append("HTC ");
                            i += 2;
                            continue;
                        }
                    }
                }
                output.Append(a);
            }
            return output.ToString();
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new ViveportLibrarySettingsView();
        }

        public override IEnumerable<PlayController> GetPlayActions(GetPlayActionsArgs args)
        {
            if (args.Game.PluginId != Id)
                yield break;

            yield return new AutomaticPlayController(args.Game)
            {
                Name = "Start via Viveport",
                Path = $"vive://runapp/{args.Game.GameId}",
                WorkingDir = args.Game.InstallDirectory,
                TrackingMode = TrackingMode.Directory,
                TrackingPath = args.Game.InstallDirectory,
            };
        }
    }
}