using Playnite.SDK;
using Playnite.SDK.Models;
using Rawg.Common;
using System.Collections.Generic;
using System.Linq;

namespace RawgLibrary
{
    public class RawgLibraryMetadataProvider : LibraryMetadataProvider
    {
        private readonly RawgApiClient client;
        private readonly string languageCode;
        private readonly ILogger logger = LogManager.GetLogger();

        public RawgLibraryMetadataProvider(RawgApiClient client, string languageCode = "eng")
        {
            this.client = client;
            this.languageCode = languageCode;
        }

        public override GameMetadata GetMetadata(Game game)
        {
            var data = client.GetGame(game.GameId);

            if (data == null)
                return new GameMetadata();

            return ToGameMetadata(data, logger, languageCode);
        }

        public static GameMetadata ToGameMetadata(RawgGame data, ILogger logger, string languageCode)
        {
            var gameMetadata = new GameMetadata
            {
                GameId = data.Id.ToString(),
                Name = data.Name,
                Description = data.Description,
                ReleaseDate = RawgMetadataHelper.ParseReleaseDate(data, logger),
                CriticScore = data.Metacritic,
                CommunityScore = RawgMetadataHelper.ParseUserScore(data.Rating),
                Platforms = data.Platforms.Select(RawgMetadataHelper.GetPlatform).ToHashSet(),
                BackgroundImage = new MetadataFile(data.BackgroundImage),
                Tags = data.Tags?.Where(t => t.Language == languageCode).Select(t => new MetadataNameProperty(t.Name)).ToHashSet<MetadataProperty>(),
                Genres = data.Genres?.Select(g => new MetadataNameProperty(g.Name)).ToHashSet<MetadataProperty>(),
                Developers = data.Developers?.Select(d => new MetadataNameProperty(d.Name)).ToHashSet<MetadataProperty>(),
                Publishers = data.Publishers?.Select(p => new MetadataNameProperty(p.Name)).ToHashSet<MetadataProperty>(),
                Links = RawgMetadataHelper.GetLinks(data),
            };

            return gameMetadata;
        }
    }
}