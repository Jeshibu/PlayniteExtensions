using Barnite.Scrapers;
using PlayniteExtensions.Common;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Barnite;

public class ScraperManager(IPlatformUtility platformUtility, IWebDownloader downloader)
{
    public IPlatformUtility PlatformUtility { get; } = platformUtility;
    public IWebDownloader Downloader { get; } = downloader;
    public List<MetadataScraper> Scrapers { get; } = [];

    public void Add<T>() where T : MetadataScraper, new()
    {
        Scrapers.Add(Get<T>());
    }

    public T Get<T>() where T : MetadataScraper, new()
    {
        var x = new T();
        x.Initialize(PlatformUtility, Downloader);
        return x;
    }

    public ScraperSettings GetDefaultSettings(MetadataScraper scraper)
    {
        return new ScraperSettings
        {
            Enabled = true,
            Name = scraper.Name,
            WebsiteUrl = scraper.WebsiteUrl,
        };
    }

    public void InitializeScraperSettingsCollection(ObservableCollection<ScraperSettings> scraperSettings)
    {
        var missing = Scrapers.Where(s => !scraperSettings.Any(x => x.Name == s.Name)).ToList();
        int max = 0;
        if(scraperSettings.Count != 0)
            max = scraperSettings.Select(x => x.Order).Max();
        foreach (var m in missing)
        {
            var add = GetDefaultSettings(m);
            add.Order = ++max;
            scraperSettings.Add(add);
        }
    }

    public List<MetadataScraper> GetOrderedListFromSettings(IEnumerable<ScraperSettings> settings)
    {
        return settings
            .Where(s => s.Enabled)
            .OrderBy(s => s.Order)
            .Select(x => Scrapers.FirstOrDefault(s => s.Name == x.Name))
            .Where(x => x != null)
            .ToList();
    }
}