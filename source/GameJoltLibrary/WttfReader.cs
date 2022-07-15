using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameJoltLibrary
{
    public class WttfReader
    {
        private static string DefaultPackagesFilePath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\game-jolt-client\User Data\Default\packages.wttf");
        private static string DefaultGamesFilePath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\game-jolt-client\User Data\Default\games.wttf");

        public WttfReader(string packagesFilePath = null, string gamesFilePath = null)
        {
            PackagesFilePath = packagesFilePath ?? DefaultPackagesFilePath;
            GamesFilePath = gamesFilePath ?? DefaultGamesFilePath;
        }

        public string PackagesFilePath { get; }
        public string GamesFilePath { get; }
        private ILogger logger = LogManager.GetLogger();

        public Dictionary<string, GameJolt.Data.Json.Package> GetPackages()
        {
            return GetWttfFileContent<GameJolt.Data.Json.Package>(PackagesFilePath);
        }
        public Dictionary<string, GameJolt.Data.Json.Game> GetGames()
        {
            return GetWttfFileContent<GameJolt.Data.Json.Game>(GamesFilePath);
        }

        public IEnumerable<GameMetadata> GetGameMetadata()
        {
            var games = GetGames();
            var packages = GetPackages();

            foreach (var gameObj in games.Values)
            {
                var gameData = new GameMetadata
                {
                    GameId = gameObj.Id.ToString(),
                    Name = gameObj.Title,
                    ReleaseDate = new ReleaseDate(new DateTime(1970, 1, 1).AddMilliseconds(gameObj.ModifiedOn)),
                    Links = new List<Link>
                    {
                        new Link("Game Jolt - Game Page", $"https://gamejolt.com/games/{gameObj.Slug}/{gameObj.Id}"),
                        new Link("Game Jolt - Developer", $"https://gamejolt.com/@{gameObj.Developer?.Username}"),
                    },
                    Developers = new HashSet<MetadataProperty> { new MetadataNameProperty(gameObj.Developer.DisplayName) },
                    Publishers = new HashSet<MetadataProperty> { new MetadataNameProperty(gameObj.Developer.DisplayName) },
                    Source = new MetadataNameProperty("Game Jolt"),
                };

                var gamePackages = packages.Values.Where(p => p.GameId == gameObj.Id);
                foreach (var p in gamePackages)
                {
                    //For multiple packages, one will have to count as the base install directory. Might as well make it the last one in the list.
                    //Game directory structure is [Library folder]\[game slug]-[game id]\[package name (or "default" if empty)]-[package id]\data\
                    //contents deeper than this are up to the dev
                    //WTTF file reported install dir is the package one
                    //By taking the parent directory here as the install dir, hopefully multiple packages (if any) will be located in the same directory
                    //If, after installing the first package, the user changes their default install directory in the GameJolt client, the install directory in Playnite will be only one of 2 (or more).
                    gameData.InstallDirectory = Directory.GetParent(p.InstallDir).FullName;
                    gameData.IsInstalled = true;
                    gameData.Icon = new MetadataFile(GetExePath(p, p.LaunchOptions.First()));
                }

                yield return gameData;
            }
        }

        public IEnumerable<GameAction> GetActions(int gameId)
        {
            var packages = GetPackages().Values;
            foreach (var package in packages)
            {
                if (package.GameId != gameId)
                    continue;

                string actionName;
                if (string.IsNullOrWhiteSpace(package.Title))
                    actionName = "Default";
                else
                    actionName = package.Title;

                var exePaths = GetExePaths(package);

                foreach (var exePath in exePaths)
                {
                    yield return new GameAction
                    {
                        Type = GameActionType.File,
                        Name = actionName,
                        IsPlayAction = true,
                        Path = exePath,
                        WorkingDir = package.InstallDir,
                    };
                }
            }
        }

        private Dictionary<string, T> GetWttfFileContent<T>(string filePath)
        {
            if (!File.Exists(filePath))
            {
                logger.Error($"{filePath} does not exist");
                throw new FileNotFoundException("GameJolt file missing", filePath);
            }

            var fileContents = File.ReadAllText(filePath);
            var root = JsonConvert.DeserializeObject<GameJolt.Data.Json.DataRoot<T>>(fileContents);

            return root.Objects;
        }

        private static string GetExePath(GameJolt.Data.Json.Package package, GameJolt.Data.Json.LaunchOption launchOption)
        {
            return Path.Combine(package.InstallDir, "data", launchOption.ExecutablePath);
        }

        private List<string> GetExePaths(GameJolt.Data.Json.Package package)
        {
            List<string> paths = new List<string>();

            if (Environment.Is64BitOperatingSystem)
                paths = GetExePaths(package, "windows_64");

            if(paths.Count == 0)
                paths = GetExePaths(package, "windows");

            if (paths.Count == 0)
                paths = GetExePaths(package, null);

            return paths;
        }

        private List<string> GetExePaths(GameJolt.Data.Json.Package package, string osFilter)
        {
            IEnumerable<GameJolt.Data.Json.LaunchOption> launchOptions = package.LaunchOptions;
            if (osFilter != null)
                launchOptions = package.LaunchOptions.Where(lo => lo.OS == osFilter);

            return launchOptions.Select(lo => GetExePath(package, lo)).ToList();
        }
    }
}

namespace GameJolt.Data.Json
{
    public class DataRoot<T>
    {
        public int Version { get; set; }
        public Dictionary<string, T> Objects { get; set; }
    }

    public class Package
    {
        public int Id { get; set; }

        [JsonProperty("game_id")]
        public int GameId { get; set; }

        public Release Release { get; set; }

        /// <summary>
        /// For some reason this is null often?
        /// </summary>
        public string Title { get; set; }

        [JsonProperty("install_dir")]
        public string InstallDir { get; set; }

        [JsonProperty("launch_options")]
        public List<LaunchOption> LaunchOptions { get; set; } = new List<LaunchOption>();
    }

    public class LaunchOption
    {
        public int Id { get; set; }

        public string OS { get; set; }

        [JsonProperty("executable_path")]
        public string ExecutablePath { get; set; }
    }

    public class Release
    {
        public int Id { get; set; }

        [JsonProperty("version_number")]
        public string VersionNumber { get; set; }
    }

    public class Game
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Title { get; set; }
        public Developer Developer { get; set; }

        [JsonProperty("modified_on")]
        public long ModifiedOn { get; set; }
    }

    public class Developer
    {
        public string Slug { get; set; }

        [JsonProperty("display_name")]
        public string DisplayName { get; set; }

        public string Username { get; set; }
    }
}