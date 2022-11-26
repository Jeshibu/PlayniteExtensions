using Playnite.SDK;
using Playnite.SDK.Models;
using Rawg.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RawgLibrary
{
    public class RawgLibraryMetadataProvider : LibraryMetadataProvider
    {
        private readonly RawgLibrarySettings settings;
        private readonly RawgApiClient client;
        private readonly string languageCode;
        private readonly ILogger logger = LogManager.GetLogger();

        public RawgLibraryMetadataProvider(RawgLibrarySettings settings, RawgApiClient client, string languageCode = "eng")
        {
            this.settings = settings;
            this.client = client;
            this.languageCode = languageCode;
        }

        public override GameMetadata GetMetadata(Game game)
        {
            var data = client.GetGame(game.GameId);

            if (data == null)
                return new GameMetadata();

            return ToGameMetadata(data, logger, languageCode, settings);
        }

        public static GameMetadata ToGameMetadata(RawgGameDetails data, ILogger logger, string languageCode, RawgLibrarySettings settings)
        {
            var gameMetadata = new GameMetadata
            {
                GameId = data.Id.ToString(),
                Name = RawgMetadataHelper.StripYear(data.Name),
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

            if (data.UserGame != null)
            {
                if (settings.RawgToPlayniteStatuses.TryGetValue(data.UserGame.Status, out Guid? statusId) && statusId.HasValue && statusId != Guid.Empty)
                    gameMetadata.CompletionStatus = new MetadataIdProperty(statusId.Value);

                if (data.UserRating != 0 && settings.RawgToPlayniteRatings.TryGetValue(data.UserRating, out int playniteRating))
                    gameMetadata.UserScore = playniteRating;
            }

            return gameMetadata;
        }
    }
}