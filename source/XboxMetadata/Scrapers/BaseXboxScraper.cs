using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace XboxMetadata.Scrapers;

public abstract class BaseXboxScraper
{
    public abstract string Key { get; }
    public abstract int ExecutionOrder { get; }
    protected IWebDownloader downloader;
    protected IPlatformUtility platformUtility;

    public BaseXboxScraper(IWebDownloader downloader, IPlatformUtility platformUtility)
    {
        this.downloader = downloader;
        this.platformUtility = platformUtility;
    }
    public abstract Task<List<XboxGameSearchResultItem>> SearchAsync(XboxMetadataSettings settings, string query);
    public abstract Task<XboxGameDetails> GetDetailsAsync(XboxMetadataSettings settings, string id, string url);
    public abstract string FixUrl(string url);
}

public class XboxGameSearchResultItem
{
    public string ScraperKey { get; set; }
    public string Id { get; set; }
    public string Title { get; set; }
    public DateTime? ReleaseDate { get; set; }
    public List<MetadataProperty> Platforms { get; set; } = [];
    public string Url { get; set; }
}

public class XboxGameDetails : XboxGameSearchResultItem
{
    public string Description { get; set; }
    public List<string> Developers { get; set; } = [];
    public List<string> Publishers { get; set; } = [];
    public int? CommunityScore { get; set; }
    public ulong? InstallSize { get; set; }
    public List<string> Genres { get; set; } = [];
    public List<string> Features { get; set; } = [];
    public List<ImageData> Covers { get; set; } = [];
    public List<ImageData> Backgrounds { get; set; } = [];
    public string AgeRating { get; set; }
    public List<Link> Links { get; set; } = [];
}

public class ImageData
{
    public string Url { get; set; }
    public string ThumbnailUrl { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
