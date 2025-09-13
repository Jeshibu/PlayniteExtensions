using AngleSharp.Dom;
using AngleSharp.Parser.Html;
using Barnite.Scrapers;
using MobyGamesMetadata.Api.V2;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace MobyGamesMetadata.Api;

public class MobyGamesScraper(IPlatformUtility platformUtility, IWebViewFactory webViewFactory) : IDisposable
{
    private IWebView _webView;
    private IWebView WebView => _webView ??= webViewFactory.CreateOffscreenView();
    private readonly MobyGamesIdUtility _mobyGamesIdUtility = new();

    private static string GetSearchUrl(string query, string objectType)
    {
        query = Uri.EscapeDataString(query);
        return $"https://www.mobygames.com/search/?q={query}&type={objectType}&adult=true";
    }

    private static string GetGameDetailsUrl(int id)
    {
        return $"https://www.mobygames.com/game/{id}";
    }

    public IEnumerable<GameSearchResult> GetGameSearchResults(string query)
    {
        var url = GetSearchUrl(query, "game");
        var pageSource = GetPageSource(url);
        return ParseGameSearchResultHtml(pageSource);
    }

    public IEnumerable<GroupSearchResult> GetGroupSearchResults(string query)
    {
        var url = GetSearchUrl(query, "group");
        var pageSource = GetPageSource(url);
        return ParseGroupSearchResultHtml(pageSource);
    }

    public GameDetails GetGameDetails(int id)
    {
        var url = GetGameDetailsUrl(id);
        return GetGameDetails(url);
    }

    public IEnumerable<GameDetails> GetGamesFromGroup(int id, GlobalProgressActionArgs progress = null) => GetGamesFromGroup($"https://www.mobygames.com/group/{id}/", progress);

    public IEnumerable<GameDetails> GetGamesFromGroup(string firstPageUrl, GlobalProgressActionArgs progress = null)
    {
        var url = firstPageUrl;
        var htmlParser = new HtmlParser();
        var pageNumberRegex = new Regex(@"\bPage (?<current>\d+) of (?<total>\d+)\b");
        var random = new Random();
        do
        {
            var doc = htmlParser.Parse(GetPageSource(url));

            var paginationElement = doc.QuerySelector("main > p:last-child");
            if (paginationElement != null && progress != null)
            {
                var match = pageNumberRegex.Match(paginationElement.TextContent);
                if (match.Success)
                {
                    progress.CurrentProgressValue = int.Parse(match.Groups["current"].Value);
                    progress.ProgressMaxValue = int.Parse(match.Groups["total"].Value);
                }
            }

            var nextPageUrl = paginationElement?.QuerySelector("a[href]:last-of-type")?.GetAttribute("href").GetAbsoluteUrl(url);

            if (nextPageUrl != null && string.Compare(nextPageUrl, url, StringComparison.InvariantCultureIgnoreCase) > 0)
                url = nextPageUrl;
            else
                url = null;

            var rows = doc.QuerySelectorAll("table.mb > tbody > tr");
            foreach (var row in rows)
            {
                var cells = row.QuerySelectorAll("> td");
                if (cells.Length < 3)
                    continue;

                var titleLink = cells[0].QuerySelector("a[href]:last-of-type");
                if (titleLink == null)
                    continue;

                var gameUrl = titleLink.GetAttribute("href");

                var gameDetails = new GameDetails
                {
                    Names = [titleLink.TextContent],
                    Url = gameUrl,
                    Id = _mobyGamesIdUtility.GetIdFromUrl(gameUrl).Id
                };

                if (titleLink.TextContent.EndsWith("..."))
                    gameDetails.Names = [gameDetails.Url.Split(['/'], StringSplitOptions.RemoveEmptyEntries).Last()];

                var releaseYearString = cells[1].TextContent;
                if (int.TryParse(releaseYearString, out var releaseYear))
                    gameDetails.ReleaseDate = new(releaseYear);

                var platformLinks = cells[2].QuerySelectorAll("a[href]");
                if (!platformLinks.Any(l => l.GetAttribute("href").Contains("mobygames.com/game/"))) // (+x more) link - platform list cannot be exhaustive, so leave it blank
                    gameDetails.Platforms.AddRange(platformLinks.Select(l => l.TextContent).SelectMany(platformUtility.GetPlatforms));

                yield return gameDetails;
            }

            if (url != null)
                Thread.Sleep(random.Next(1000, 4000));
        } while (url != null);
    }

    private GameDetails GetGameDetails(string url)
    {
        var pageSource = GetPageSource(url);
        var details = ParseGameDetailsHtml(pageSource);
        details.Url = url;
        return details;
    }

    private string GetPageSource(string url)
    {
        WebView.NavigateAndWait(url);
        return WebView.GetPageSource();
    }

    private IEnumerable<GameSearchResult> ParseGameSearchResultHtml(string html)
    {
        var doc = new HtmlParser().Parse(html);

        var cells = doc.QuerySelectorAll("table.table.mb > tbody > tr > td:last-of-type");
        if (cells == null)
            yield break;

        foreach (var td in cells)
        {
            var a = td.QuerySelector("a[href]");
            if (a == null)
                continue;

            var releaseDateString = td.QuerySelectorAll("> span")
                                      .Select(span => span.TextContent.HtmlDecode())
                                      .FirstOrDefault(s => s.StartsWith("("));

            var platforms = td.QuerySelector("small:last-of-type").ChildNodes
                              .Where(n => n.NodeType == NodeType.Text)
                              .Select(n => n.TextContent.HtmlDecode())
                              .Where(str => !string.IsNullOrWhiteSpace(str))
                              .ToList();

            var alternateNames = td.Children.FirstOrDefault(n => n.TextContent.StartsWith("AKA: "))
                                   ?.TextContent.TrimStart("AKA: ")
                                   .Split([", "], StringSplitOptions.RemoveEmptyEntries)
                                   .Select(x => x.Trim());

            var sr = new GameSearchResult
            {
                PlatformNames = platforms,
                ReleaseDate = MobyGamesHelper.ParseReleaseDate(releaseDateString),
            };
            sr.SetUrlAndId(a.Attributes["href"].Value);
            sr.SetName(a.TextContent.HtmlDecode(), alternateNames);
            sr.SetDescription(releaseDateString, platforms);
            yield return sr;
        }
    }

    private IEnumerable<GroupSearchResult> ParseGroupSearchResultHtml(string html)
    {
        var doc = new HtmlParser().Parse(html);

        var cells = doc.QuerySelectorAll("table.table.mb > tbody > tr > td:last-of-type");
        if (cells == null)
            yield break;

        foreach (var td in cells)
        {
            var a = td.QuerySelector("a[href]");
            if (a == null)
                continue;

            var description = td.QuerySelector("> span:last-of-type")?.TextContent.HtmlDecode();

            var sr = new GroupSearchResult { Name = a.TextContent.HtmlDecode(), Description = description };
            sr.SetUrlAndId(a.GetAttribute("href"));
            yield return sr;
        }
    }

    private GameDetails ParseGameDetailsHtml(string html)
    {
        return new MobyGamesHelper(platformUtility).ParseGameDetailsHtml(html, parseGenres: false);
    }

    public void Dispose()
    {
        if (_webView == null)
            return;

        _webView.Dispose();
        _webView = null;
    }

    ~MobyGamesScraper()
    {
        Dispose();
    }
}

public class SearchResult : GenericItemOption, IHasName
{
    public string Url { get; set; }
    public int Id { get; set; }

    public void SetUrlAndId(string url)
    {
        Url = url;
        if (url == null) return;
        var urlSegment = url.Split(['/'], StringSplitOptions.RemoveEmptyEntries).Where(x => x.All(char.IsNumber)).FirstOrDefault();
        if (urlSegment != null)
            Id = int.Parse(urlSegment);
    }
}

public class GroupSearchResult : SearchResult
{
}

public class GameSearchResult : SearchResult, IGameSearchResult
{
    public List<string> PlatformNames { get; set; } = [];
    public List<string> AlternateTitles { get; set; } = [];
    public string Title { get; set; }
    public ReleaseDate? ReleaseDate { get; set; }

    public MobyGame ApiGameResult { get; private set; }

    public IEnumerable<string> AlternateNames => AlternateTitles;

    IEnumerable<string> IGameSearchResult.Platforms => PlatformNames;

    public void SetName(string title, IEnumerable<string> alternateTitles)
    {
        Title = title;
        AlternateTitles = alternateTitles?.ToList() ?? [];
        if (AlternateTitles.Any())
            Name = $"{Title} (AKA {string.Join("/", AlternateTitles)})";
        else
            Name = Title;
    }

    public void SetDescription(string releaseDate, List<string> platforms)
    {
        var descriptionElements = new List<string>();
        if (!string.IsNullOrWhiteSpace(releaseDate))
            descriptionElements.Add(releaseDate);

        if (platforms != null && platforms.Any())
            descriptionElements.Add(string.Join(", ", platforms));

        Description = string.Join(" | ", descriptionElements);
    }

    public GameSearchResult()
    {
    }

    public GameSearchResult(MobyGame game)
    {
        ApiGameResult = game;
        Id = game.game_id;
        Url = game.moby_url;
        SetName(game.title, game.highlights);
        PlatformNames.AddRange(game.platforms.Select(p => p.name));
        ReleaseDate = game.release_date.ParseReleaseDate();
        SetDescription(game.release_date, PlatformNames);
    }
}