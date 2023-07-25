using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace XboxMetadata.Scrapers
{
    public class ScraperManager
    {
        public Dictionary<string, BaseXboxScraper> Scrapers { get; set; } = new Dictionary<string, BaseXboxScraper>();

        public ScraperManager(IEnumerable<BaseXboxScraper> scrapers)
        {
            Initialize(scrapers);
        }

        public ScraperManager(IWebDownloader downloader, IPlatformUtility platformUtility)
        {
            Initialize(FindAllDerivedTypes<BaseXboxScraper>().Select(t => (BaseXboxScraper)t.GetConstructor(new[] { typeof(IWebDownloader), typeof(IPlatformUtility) }).Invoke(new object[] { downloader, platformUtility })));
        }

        public IEnumerable<XboxGameSearchResultItem> Search(XboxMetadataSettings settings, Game game, string searchString, bool onlyExactMatches = false)
        {
            var tasks = Scrapers.Values.OrderBy(s => s.ExecutionOrder).Select(s => s.SearchAsync(settings, searchString)).ToArray();
            SortableNameConverter snc = new SortableNameConverter(new[] { "the", "a", "an" });
            var searchNameNormalized = snc.Convert(searchString).Deflate();
            Task.WaitAll(tasks, 30000);
            var perfectMatches = new List<XboxGameSearchResultItem>();
            var titleMatches = new List<XboxGameSearchResultItem>();
            var titleContained = new List<XboxGameSearchResultItem>();
            var otherSearchResults = new List<XboxGameSearchResultItem>();
            foreach (var t in tasks.Where(x => x.Status == TaskStatus.RanToCompletion))
            {
                foreach (var searchResultItem in t.Result)
                {
                    var searchResultNameNormalized = snc.Convert(searchResultItem.Title).Deflate();
                    bool nameMatched = searchNameNormalized.Equals(searchResultNameNormalized, StringComparison.InvariantCultureIgnoreCase);

                    if (nameMatched && HasPlatformOverlap(game, searchResultItem))
                        perfectMatches.Add(searchResultItem);
                    else if (nameMatched)
                        titleMatches.Add(searchResultItem);
                    else if (searchResultNameNormalized.Contains(searchNameNormalized, StringComparison.InvariantCultureIgnoreCase))
                        titleContained.Add(searchResultItem);
                    else
                        otherSearchResults.Add(searchResultItem);
                }
            }

            var output = new List<XboxGameSearchResultItem>();
            output.AddRange(perfectMatches);
            output.AddRange(titleMatches);
            if (!onlyExactMatches)
            {
                output.AddRange(titleContained);
                output.AddRange(otherSearchResults);
            }
            return output;
        }

        private static bool HasPlatformOverlap(Game game, XboxGameSearchResultItem searchResultItem)
        {
            if (game.Platforms == null || !game.Platforms.Any() || !searchResultItem.Platforms.Any())
                return false;

            foreach (var mp in searchResultItem.Platforms)
            {
                foreach (var platform in game.Platforms)
                {
                    if (mp is MetadataSpecProperty specPlatform)
                    {
                        if (specPlatform.Id == platform.SpecificationId)
                            return true;
                    }
                    else if (mp is MetadataNameProperty namePlatform)
                    {
                        if (namePlatform.Name.Equals(platform.Name, StringComparison.InvariantCultureIgnoreCase))
                            return true;
                    }
                }
            }
            return false;
        }

        public XboxGameDetails GetDetails(XboxMetadataSettings settings, XboxGameSearchResultItem searchResultItem)
        {
            var scraper = Scrapers[searchResultItem.ScraperKey];
            var url = scraper.FixUrl(searchResultItem.Url);
            var task = scraper.GetDetailsAsync(settings, searchResultItem.Id, url);
            task.Wait();
            return task.Result;
        }

        private void Initialize(IEnumerable<BaseXboxScraper> scrapers)
        {
            foreach (var s in scrapers)
            {
                Scrapers.Add(s.Key, s);
            }
        }

        private static List<Type> FindAllDerivedTypes<T>()
        {
            var baseType = typeof(T);
            return Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t =>
                    t != baseType &&
                    baseType.IsAssignableFrom(t) &&
                    t.GetConstructor(new[] { typeof(IWebDownloader), typeof(IPlatformUtility) }) != null
                    ).ToList();

        }
    }
}
