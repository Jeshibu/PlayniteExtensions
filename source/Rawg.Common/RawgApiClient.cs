using Newtonsoft.Json;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace Rawg.Common
{
    public class RawgApiClient
    {

        public RawgApiClient(IWebDownloader downloader, string key)
        {
            Downloader = downloader;
            Key = HttpUtility.UrlEncode(key);
        }

        public IWebDownloader Downloader { get; }
        public string Key { get; }

        public RawgGame GetGame(string slug)
        {
            var response = Downloader.DownloadString($"https://api.rawg.io/api/games/{slug}?key={Key}", throwExceptionOnErrorResponse: true);
            var output = JsonConvert.DeserializeObject<RawgGame>(response.ResponseContent);
            return output;
        }

        private T Get<T>(string url)
        {
            var response = Downloader.DownloadString(url, throwExceptionOnErrorResponse: true);
            var output = JsonConvert.DeserializeObject<T>(response.ResponseContent);
            return output;
        }

        public RawgResult<RawgSearchResultGame> SearchGames(string query)
        {
            return Get<RawgResult<RawgSearchResultGame>>($"https://rawg.io/api/games?key={Key}&search={HttpUtility.UrlEncode(query)}");
        }

        public RawgResult<RawgCollection> GetCollections(string username)
        {
            return Get<RawgResult<RawgCollection>>($"https://rawg.io/api/users/{username}/collections?key={Key}");
        }

        public RawgResult<RawgGame> GetCollectionGames(string collectionSlug)
        {
            return Get<RawgResult<RawgGame>>($"https://rawg.io/api/collections/{collectionSlug}/games?key={Key}");
        }

        public RawgResult<RawgGame> GetUserLibrary(string username)
        {
            return Get<RawgResult<RawgGame>>($"https://rawg.io/api/users/{username}/games?key={Key}");
        }
    }
}
