using AngleSharp.Parser.Html;
using Playnite.SDK;
using Playnite.SDK.Data;
using PluginsCommon.Web;
using SteamCommon.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace SteamCommon
{
    class SteamWeb
    {
        private static ILogger logger = LogManager.GetLogger();
        private const string steamGameSearchUrl = @"https://store.steampowered.com/search/?term={0}&ignore_preferences=1&category1=998%2C997";

        public static List<GenericItemOption> GetSteamSearchGenericItemOptions(string searchTerm)
        {
            return GetSteamSearchResults(searchTerm).Select(x => new GenericItemOption(x.Name, x.GameId)).ToList();
        }

        public static string GetSteamIdFromSearch(string searchTerm)
        {
            var normalizedName = searchTerm.NormalizeGameName();
            var results = GetSteamSearchResults(normalizedName);
            results.ForEach(a => a.Name = a.Name.NormalizeGameName());

            var matchingGameName = normalizedName.GetMatchModifiedName();
            var exactMatch = results.FirstOrDefault(x => x.Name.GetMatchModifiedName() == matchingGameName);
            if (exactMatch != null)
            {
                logger.Info($"Found steam id for search {searchTerm} via steam search, Id: {exactMatch.GameId}");
                return exactMatch.GameId;
            }

            logger.Info($"Steam id for search {searchTerm} not found");
            return null;
        }

        public static List<StoreSearchResult> GetSteamSearchResults(string searchTerm, string steamApiCountry = null)
        {
            var results = new List<StoreSearchResult>();
            var searchPageSrc = HttpDownloader.DownloadStringAsync(GetStoreSearchUrl(searchTerm, steamApiCountry)).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(searchPageSrc))
            {
                var parser = new HtmlParser();
                var searchPage = parser.Parse(searchPageSrc);
                foreach (var gameElem in searchPage.QuerySelectorAll(".search_result_row"))
                {
                    if (gameElem.HasAttribute("data-ds-packageid"))
                    {
                        continue;
                    }

                    // Game Data
                    var title = gameElem.QuerySelector(".title").InnerHtml;
                    var releaseDate = gameElem.QuerySelector(".search_released").InnerHtml;
                    var gameId = gameElem.GetAttribute("data-ds-appid");

                    //Urls
                    var storeUrl = gameElem.GetAttribute("href");
                    var capsuleUrl = gameElem.QuerySelector(".search_capsule").Children[0].GetAttribute("src");

                    results.Add(new StoreSearchResult
                    {
                        Name = HttpUtility.HtmlDecode(title),
                        Description = HttpUtility.HtmlDecode(releaseDate),
                        GameId = gameId,
                        StoreUrl = storeUrl,
                        BannerImageUrl = capsuleUrl
                    });
                }
            }

            logger.Debug($"Obtained {results.Count} games from Steam search term {searchTerm}");
            return results;
        }

        private static string GetStoreSearchUrl(string searchTerm, string steamApiCountry)
        {
            var searchUrl = string.Format(steamGameSearchUrl, searchTerm);
            if (!steamApiCountry.IsNullOrEmpty())
            {
                searchUrl += $"&cc={steamApiCountry}";
            }

            return searchUrl;
        }

        private const string steamAppDetailsMask = @"https://store.steampowered.com/api/appdetails?appids={0}";
        public static SteamAppDetails GetSteamAppDetails(string steamId)
        {
            var url = string.Format(steamAppDetailsMask, steamId);
            var downloadedString = HttpDownloader.DownloadStringAsync(url).GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(downloadedString))
            {
                var parsedData = Serialization.FromJson<Dictionary<string, SteamAppDetails>>(downloadedString);
                if (parsedData.Keys?.Any() == true)
                {
                    var response = parsedData[parsedData.Keys.First()];
                    if (response.success == true && response.data != null)
                    {
                        return response;
                    }
                }
            }

            return null;
        }
    }
}