using Playnite;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;

namespace PlayniteExtensions.Tests.Common
{
    public class FakePlayniteDatabase: IGameDatabaseAPI
    {
        public Game ImportGame(GameMetadata game)
        {
            throw new NotImplementedException();
        }

        public Game ImportGame(GameMetadata game, LibraryPlugin sourcePlugin)
        {
            throw new NotImplementedException();
        }

        public bool GetGameMatchesFilter(Game game, FilterPresetSettings filterSettings)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Game> GetFilteredGames(FilterPresetSettings filterSettings)
        {
            throw new NotImplementedException();
        }

        public bool GetGameMatchesFilter(Game game, FilterPresetSettings filterSettings, bool useFuzzyNameMatch)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Game> GetFilteredGames(FilterPresetSettings filterSettings, bool useFuzzyNameMatch)
        {
            throw new NotImplementedException();
        }

        public IItemCollection<Game> Games { get; } = new FakeItemCollection<Game>();
        public IItemCollection<Platform> Platforms { get; }
        public IItemCollection<Emulator> Emulators { get; }
        public IItemCollection<Genre> Genres { get; }
        public IItemCollection<Company> Companies { get; }
        public IItemCollection<Tag> Tags { get; }
        public IItemCollection<Category> Categories { get; }
        public IItemCollection<Series> Series { get; }
        public IItemCollection<AgeRating> AgeRatings { get; }
        public IItemCollection<Region> Regions { get; }
        public IItemCollection<GameSource> Sources { get; }
        public IItemCollection<GameFeature> Features { get; }
        public IItemCollection<GameScannerConfig> GameScanners { get; }
        public IItemCollection<CompletionStatus> CompletionStatuses { get; }
        public IItemCollection<ImportExclusionItem> ImportExclusions { get; }
        public IItemCollection<FilterPreset> FilterPresets { get; }
        public bool IsOpen { get; }
        public event EventHandler DatabaseOpened;
        public string AddFile(string path, Guid parentId)
        {
            throw new NotImplementedException();
        }

        public void SaveFile(string id, string path)
        {
            throw new NotImplementedException();
        }

        public void RemoveFile(string id)
        {
            throw new NotImplementedException();
        }

        public IDisposable BufferedUpdate()
        {
            throw new NotImplementedException();
        }

        public void BeginBufferUpdate()
        {
            throw new NotImplementedException();
        }

        public void EndBufferUpdate()
        {
            throw new NotImplementedException();
        }

        public string GetFileStoragePath(Guid parentId)
        {
            throw new NotImplementedException();
        }

        public string GetFullFilePath(string databasePath)
        {
            throw new NotImplementedException();
        }

        public string DatabasePath { get; }
    }
}
