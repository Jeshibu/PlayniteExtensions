using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace OpenCriticMetadata
{
    public class OpenCriticMetadataProvider : GenericMetadataProvider<OpenCriticSearchResultItem>
    {
        private readonly OpenCriticMetadata plugin;

        public override List<MetadataField> AvailableFields => plugin.SupportedFields;

        protected override string ProviderName { get; } = "OpenCritic";

        public OpenCriticMetadataProvider(MetadataRequestOptions options, OpenCriticMetadata plugin, IGameSearchProvider<OpenCriticSearchResultItem> gameSearchProvider, IPlatformUtility platformUtility)
            : base(gameSearchProvider, options, plugin.PlayniteApi, platformUtility)
        {
            this.plugin = plugin;
        }
    }

    public class OpenCriticSearchProvider : IGameSearchProvider<OpenCriticSearchResultItem>
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private readonly IPlatformUtility platformUtility;
        private readonly RestClient restClient = new RestClient("https://api.opencritic.com/api/")
            .AddDefaultHeader("Referer", "https://opencritic.com/");

        public OpenCriticSearchProvider(IPlatformUtility platformUtility)
        {
            this.platformUtility = platformUtility;
        }

        public GameDetails GetDetails(OpenCriticSearchResultItem searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
        {
            var request = new RestRequest("game/{id}").AddUrlSegment("id", searchResult.Id);
            var result = Execute<OpenCriticGame>(request);
            return ToGameDetails(result);
        }

        public IEnumerable<OpenCriticSearchResultItem> Search(string query, CancellationToken cancellationToken = default)
        {
            var request = new RestRequest("meta/search").AddQueryParameter("criteria", query.Replace(' ', '+'));
            var result = Execute<OpenCriticSearchResultItem[]>(request);
            return result?.Where(x => x.Relation == "game");
        }

        public GenericItemOption<OpenCriticSearchResultItem> ToGenericItemOption(OpenCriticSearchResultItem item)
        {
            return new GenericItemOption<OpenCriticSearchResultItem>(item) { Name = item.Name };
        }

        public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
        {
            gameDetails = null;
            return false;
        }

        private GameDetails ToGameDetails(OpenCriticGame game)
        {
            if (game == null)
                return null;

            var output = new GameDetails
            {
                Platforms = game.Platforms.SelectMany(p => this.platformUtility.GetPlatforms(p.Name)).ToList(),
                Id = game.Id.ToString(),
                Url = game.Url,
                CommunityScore = (int)Math.Round(game.MedianScore),
                CriticScore = (int)Math.Round(game.TopCriticScore),
            };

            if (!string.IsNullOrWhiteSpace(game.Description))
                output.Description = Regex.Replace(game.Description, @"\r?\n", "<br>$0");

            if (output.CommunityScore < 1) output.CommunityScore = null;
            if (output.CriticScore < 1) output.CriticScore = null;

            output.Names.Add(game.Name);
            output.Developers.AddRange(GetCompanies(game, "DEVELOPER"));
            output.Publishers.AddRange(GetCompanies(game, "PUBLISHER"));

            if (game.Genres != null)
                output.Genres.AddRange(game.Genres.Select(g => g.Name));

            if (game.Images?.Box?.OG != null)
                output.CoverOptions.Add(game.Images.Box);

            if (game.Images?.Square?.OG != null)
                output.CoverOptions.Add(game.Images.Square);

            if (game.Images?.Masthead?.OG != null)
                output.BackgroundOptions.Add(game.Images.Masthead);

            if (game.Images?.Screenshots.Count > 0)
                output.BackgroundOptions.AddRange(game.Images.Screenshots);

            if (game.FirstReleaseDate.HasValue)
                output.ReleaseDate = new ReleaseDate(game.FirstReleaseDate.Value.LocalDateTime);

            return output;
        }

        private static IEnumerable<string> GetCompanies(OpenCriticGame game, string type)
        {
            if (game?.Companies == null)
                return new string[0];

            return game.Companies.Where(c => string.Equals(c.Type, type, StringComparison.InvariantCultureIgnoreCase)).Select(c => c.Name);
        }

        private T Execute<T>(RestRequest request) where T : class
        {
            try
            {
                var response = restClient.Execute(request);
                if (!string.IsNullOrEmpty(response.ErrorMessage))
                {
                    logger.Error(response.ErrorMessage);
                }
                var data = JsonConvert.DeserializeObject<T>(response.Content);
                return data;
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error getting {request.Resource}");
                return null;
            }
        }
    }

    public class OpenCriticBaseModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class OpenCriticSearchResultItem : OpenCriticBaseModel, IGameSearchResult
    {
        public string Relation { get; set; }

        string IGameSearchResult.Title => Name;

        IEnumerable<string> IGameSearchResult.AlternateNames => new string[0];

        IEnumerable<string> IGameSearchResult.Platforms => new string[0];

        ReleaseDate? IGameSearchResult.ReleaseDate => null;
    }

    public class OpenCriticGame : OpenCriticBaseModel
    {
        public bool? HasLootboxes { get; set; }
        public bool IsMajorRelease { get; set; }
        public OpenCriticImageCollection Images { get; set; }
        public int NumReviews { get; set; }
        public int NumTopCriticReviews { get; set; }
        public double MedianScore { get; set; }
        public double TopCriticScore { get; set; }
        public double Percentile { get; set; }
        public double PercentRecommended { get; set; }
        public string Description { get; set; }
        public DateTimeOffset? FirstReleaseDate { get; set; }
        public List<OpenCriticCompany> Companies { get; set; } = new List<OpenCriticCompany>();
        public List<OpenCriticBaseModel> Genres { get; set; } = new List<OpenCriticBaseModel>();
        public List<OpenCriticPlatform> Platforms { get; set; } = new List<OpenCriticPlatform>();
        public string Url { get; set; }
    }

    public class OpenCriticImageCollection
    {
        public OpenCriticImage Box { get; set; }
        public OpenCriticImage Square { get; set; }
        public OpenCriticImage Masthead { get; set; }
        public List<OpenCriticImage> Screenshots { get; set; } = new List<OpenCriticImage>();
    }

    public class OpenCriticImage : IImageData
    {
        public string OG { get; set; }
        public string SM { get; set; }

        string IImageData.Url => AddDomain(OG);

        string IImageData.ThumbnailUrl => AddDomain(SM);

        int IImageData.Width => 0;

        int IImageData.Height => 0;

        IEnumerable<string> IImageData.Platforms => new string[0];

        private static string AddDomain(string path) => path == null ? null : $"https://img.opencritic.com/{path}";
    }

    public class OpenCriticCompany
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    public class OpenCriticPlatform : OpenCriticBaseModel
    {
        public string ShortName { get; set; }
        public DateTimeOffset ReleaseDate { get; set; }
    }
}