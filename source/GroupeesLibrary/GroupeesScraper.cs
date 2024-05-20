using HtmlAgilityPack;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace GroupeesLibrary
{
    public class GroupeesScraper
    {
        private ILogger logger = LogManager.GetLogger();

        public string GetAuthenticatedCsrfToken(IWebDownloader downloader)
        {
            string purchasesUrl = "https://groupees.com/purchases";
            var response = downloader.DownloadString(purchasesUrl);
            if (response.ResponseUrl != purchasesUrl)
                return null;
            var match = Regex.Match(response.ResponseContent, @"<meta\s*name=""csrf-token""\s*content=""(.+)""\s*/>");
            if (match.Success)
                return match.Groups[1].Value;
            else
                return null;
        }

        private static Regex DownloadUrlGameIdRegex = new Regex(@"https://storage\.groupees\.com/games/(?<gameid>[0-9]+)/download/", RegexOptions.Compiled);

        public IEnumerable<GameMetadata> GetGames(GroupeesLibrarySettings settings, IWebDownloader downloader, string csrfToken)
        {
            var orderRows = GetAllOrderRows(settings, downloader, csrfToken);
            foreach (var orderRow in orderRows)
            {
                if (!orderRow.Revealed)
                {
                    //non-revealed games cannot be downloaded, which means we can't get the game ID from the download URL(s)
                    //also non-revealed games might be given/traded away via the Groupees website itself, so strictly speaking they're not owned yet
                    logger.Debug($"Skipped {orderRow.Title} because it's not revealed");
                    continue; 
                }

                if (orderRow.Title.EndsWith(" DLC") || orderRow.Title.EndsWith(" DLCs"))
                {
                    logger.Debug($"Skipped {orderRow.Title} because it's assumed to be DLC only based on the title");
                    continue; //only interested in games here, not DLC
                }

                var details = GetDetails(settings, downloader, orderRow.Id, csrfToken);

                if (details.DownloadUrlsByPlatform.Count == 0)
                {
                    logger.Debug($"Skipped {orderRow.Title} because it has no downloads available");
                    continue; //can't find the ID of games without downloads so ignore them
                }

                var match = DownloadUrlGameIdRegex.Match(details.DownloadUrlsByPlatform.Values.First());
                if (!match.Success)
                {
                    logger.Info($"Couldn't find game ID in download URL {details.DownloadUrlsByPlatform.Values.First()}");
                    continue;
                }

                var metadata = new GameMetadata
                {
                    GameId = match.Groups["gameid"].Value,
                    Name = details.Title,
                    Description = details.Description,
                    Platforms = new HashSet<MetadataProperty>(GetPlatforms(details.DownloadUrlsByPlatform.Keys)),
                    Source = new MetadataNameProperty("Groupees"),
                };

                if (details.DownloadUrlsByPlatform.TryGetValue("PC", out string downloadUrl))
                {
                    if (settings.InstallData.TryGetValue(metadata.GameId, out var installData))
                    {
                        installData.DownloadUrl = downloadUrl;
                        if (!string.IsNullOrEmpty(installData.InstallLocation) && Directory.Exists(installData.InstallLocation))
                        {
                            metadata.InstallDirectory = installData.InstallLocation;
                            metadata.IsInstalled = true;
                        }
                    }
                    else
                    {
                        settings.InstallData.Add(metadata.GameId, new GameInstallInfo
                        {
                            Name = details.Title,
                            DownloadUrl = downloadUrl,
                        });
                    }
                }
                else
                {
                    logger.Info($"No PC platform found in downloads for {metadata.Name}. Available platforms: {string.Join(", ", details.DownloadUrlsByPlatform.Keys)}");
                }

                yield return metadata;
            }
        }

        public List<GroupeesOrderRow> GetAllOrderRows(GroupeesLibrarySettings settings, IWebDownloader downloader, string csrfToken)
        {
            var all = new List<GroupeesOrderRow>();
            List<GroupeesOrderRow> lastIterationResults;
            int i = 1;
            do
            {
                lastIterationResults = GetOrderRows(settings, downloader, i, csrfToken);
                all.AddRange(lastIterationResults);
                i++;
            } while (lastIterationResults.Count > 0);

            return all;
        }

        public List<GroupeesOrderRow> GetOrderRows(GroupeesLibrarySettings settings, IWebDownloader downloader, int page, string csrfToken)
        {
            var response = downloader.DownloadString($"https://groupees.com/users/{settings.UserId}/more_entries?page={page}&kind=games&filters%5Bkey%5D%5B%5D=drm-free", referer: "https://groupees.com/purchases", headerSetter: GetHeaderSetter(csrfToken));
            if (string.IsNullOrWhiteSpace(response.ResponseContent))
                return new List<GroupeesOrderRow>();
            var strings = JsonConvert.DeserializeObject<string[]>(response.ResponseContent);
            return strings.Select(JsonConvert.DeserializeObject<GroupeesOrderRow>).ToList();
        }

        public GroupeesGameDetails GetDetails(GroupeesLibrarySettings settings, IWebDownloader downloader, int gameId, string csrfToken)
        {
            var url = $"https://groupees.com/user_products/{gameId}?user_id={settings.UserId}&_={DateTimeOffset.Now.ToUnixTimeSeconds()}";
            var response = downloader.DownloadString(url, referer: "https://groupees.com/purchases", headerSetter: GetHeaderSetter(csrfToken));

            var articleMatch = Regex.Match(response.ResponseContent, @"^article\.html\('(.+)'\);$", RegexOptions.Multiline);
            if (!articleMatch.Success)
                throw new Exception($"Get details for game {gameId} failed");

            string article = articleMatch.Groups[1].Value;

            article = Regex.Replace(article, @"\\.", (match) =>
            {
                switch (match.Value)
                {
                    case @"\n": return "\n";
                    case @"\t": return "\t";
                    default: return match.Value.Substring(1);
                }
            });

            var game = new GroupeesGameDetails { ProductId = gameId };

            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(article);
            game.Title = doc.DocumentNode.SelectSingleNode("//h3[starts-with(@class, 'product-name')]")?.InnerText.HtmlDecode();
            game.Description = doc.DocumentNode.SelectSingleNode("//div[@class='product-info']")?.InnerHtml;

            if (game.Description != null)
            {
                //remove system requirements and links
                int tableIndex = game.Description.IndexOf("</table>");
                if (tableIndex != -1)
                    game.Description = game.Description.Substring(tableIndex + "</table>".Length).Trim();
            }

            var nodes = doc.DocumentNode.SelectNodes("//li[@role='presentation']");
            if (nodes != null)
            {
                foreach (var n in nodes)
                {
                    string platform = n.InnerText.Trim();
                    var downloadUrl = n.SelectSingleNode("./following-sibling::li[1]/a[@href]")?.Attributes["href"].Value;
                    if (platform != null && downloadUrl != null)
                        game.DownloadUrlsByPlatform.Add(platform, downloadUrl);
                }
            }
            return game;
        }

        private Action<WebHeaderCollection> GetHeaderSetter(string csrfToken)
        {
            return (WebHeaderCollection header) =>
            {
                header["X-CSRF-Token"] = csrfToken;
                header["X-Requested-With"] = "XMLHttpRequest";
            };
        }

        private IEnumerable<MetadataProperty> GetPlatforms(IEnumerable<string> platformNames)
        {
            bool noPlatforms = true;
            foreach (var name in platformNames)
            {
                noPlatforms = false;
                switch (name.ToLowerInvariant())
                {
                    case "pc": yield return new MetadataSpecProperty("pc_windows"); break;
                    case "os x": yield return new MetadataSpecProperty("macintosh"); break;
                    case "linux": yield return new MetadataSpecProperty("pc_linux"); break;
                    default: continue;
                }
            }

            if (noPlatforms)
                yield return new MetadataSpecProperty("pc_windows");
        }
    }

    //{"id":3108251,"product_type":"Game","is_favorite":false,"revealed":true,"title":"8-Bit Commando","artist":"2D Engine"}
    public class GroupeesOrderRow
    {
        public int Id;
        public string Title;
        public string Artist;
        public bool Revealed;
    }

    public class GroupeesGameDetails
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Dictionary<string, string> DownloadUrlsByPlatform { get; set; } = new Dictionary<string, string>();
    }
}
