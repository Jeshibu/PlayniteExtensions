using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BigFishMetadata;

public class BigFishSearchProvider(IWebDownloader downloader, BigFishMetadataSettings settings) : IGameSearchProvider<BigFishSearchResultGame>
{
    private readonly BigFishGraphQLService _service = new(downloader);

    public IEnumerable<BigFishSearchResultGame> Search(string query, CancellationToken cancellationToken = default)
    {
        var result = _service.Search(query, settings.SelectedLanguage);
        foreach (var item in result.products.items)
        {
            var game = new BigFishSearchResultGame
            {
                Id = item.id,
                Name = item.name,
                UrlKey = item.url_key,
                CoverUrl = item.small_image?.url,
                RatingsSummary = item.rating_summary,
            };

            if (Enum.IsDefined(typeof(BigFishPlatform), item.platform))
                game.Platform = ((BigFishPlatform)item.platform).ToString();

            if (DateTime.TryParse(item.product_list_date, out var releaseDate))
                game.ReleaseDate = new(releaseDate);

            yield return game;
        }
    }

    public GenericItemOption<BigFishSearchResultGame> ToGenericItemOption(BigFishSearchResultGame item) =>
        new(item) { Name = item.Name, Description = $"{item.Platform} | {item.ReleaseDate}" };

    public GameDetails GetDetails(BigFishSearchResultGame searchResult, GlobalProgressActionArgs progressArgs = null, Game searchGame = null)
    {
        if (searchResult == null) return null;

        var reviewFetchTask = GetCommunityScoreAsync(searchResult);
        var productDetails = _service.GetProductDetails(searchResult.UrlKey).products.items.First();

        var output = new GameDetails
        {
            Url = searchResult.Url,
            Links = [new Link { Name = "Big Fish Games", Url = searchResult.Url }],
            Names = [productDetails.name],
            Description = productDetails.description.html,
            CoverOptions = [new BasicImage(productDetails.small_image.url)],
        };

        var bullets = new List<string>();
        foreach (var attribute in productDetails.custom_attributes)
        {
            var code = attribute.attribute_metadata.code;
            var value = attribute.selected_attribute_options.attribute_option?.FirstOrDefault()?.label ?? attribute.entered_attribute_value.value;
            switch (code)
            {
                case "game_developer_name":
                    output.Developers.Add(value);
                    break;
                case "genre":
                case "additional_genres":
                    output.Genres.Add(value);
                    break;
                case "series":
                case "game_series":
                    output.Series.Add(value);
                    break;
                case "product_list_date":
                    if (DateTime.TryParse(value, out var releaseDate))
                        output.ReleaseDate = new(releaseDate);
                    break;
                case "sys_req_hd":
                    if (ulong.TryParse(value, out var megaBytes))
                        output.InstallSize = megaBytes * 1024 * 1024;
                    break;
                case "image_80x80_url":
                    output.IconOptions.Add(new BasicImage(value) { Height = 80, Width = 80 });
                    break;
            }

            if (code.StartsWith("bullet_"))
                bullets.Add(value);

            if (code.StartsWith("screen") && code.EndsWith("_url"))
                output.BackgroundOptions.Add(new BasicImage(value));
        }

        foreach (var category in productDetails.categories)
        {
            if (category.url_path.Contains("genre") && category.url_path.Count(c => c == '/') == 1 && !output.Genres.Contains(category.name))
                output.Genres.Add(category.name);
        }

        if (bullets.Any())
        {
            var description = new StringBuilder();
            if (output.Description != null)
                description.AppendLine(output.Description);

            description.AppendLine("<ul>");

            foreach (var bullet in bullets)
                description.AppendLine($"   <li>{bullet}</li>");

            description.AppendLine("</ul>");
            output.Description = description.ToString();
        }

        output.CommunityScore = reviewFetchTask.Result;

        return output;
    }

    private async Task<int?> GetCommunityScoreAsync(BigFishSearchResultGame searchResult)
    {
        if (settings.CommunityScoreType == CommunityScoreType.StarRating && searchResult.RatingsSummary.HasValue)
            return searchResult.RatingsSummary;

        var response = await _service.GetReviewsAsync(searchResult.Id);
        return settings.CommunityScoreType switch
        {
            CommunityScoreType.StarRating => (int)Math.Round(response.advreview.ratingSummaryValue * 20),
            CommunityScoreType.PercentageRecommended => response.advreview.recomendedPercent,
            _ => null,
        };
    }

    public bool TryGetDetails(Game game, out GameDetails gameDetails, CancellationToken cancellationToken)
    {
        gameDetails = null;
        return false;
    }
}

public class BigFishSearchResultGame : IGameSearchResult
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Platform { get; set; }

    public string CoverUrl { get; set; }

    public string UrlKey { get; set; }

    public int? RatingsSummary { get; set; }

    public ReleaseDate? ReleaseDate { get; set; }

    public string Url => $"https://www.bigfishgames.com/{UrlKey}.html";

    string IGameSearchResult.Title => Name;

    IEnumerable<string> IGameSearchResult.AlternateNames => [];

    IEnumerable<string> IGameSearchResult.Platforms => string.IsNullOrWhiteSpace(Platform) ? [] : [Platform];
}
