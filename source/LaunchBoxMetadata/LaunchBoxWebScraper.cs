using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AngleSharp.Parser.Html;
using PlayniteExtensions.Common;

namespace LaunchBoxMetadata;

public class LaunchBoxWebScraper(IWebDownloader downloader)
{
    public string GetLaunchBoxGamesDatabaseUrl(long databaseId)
    {
        var redirectRequestUrl = $"https://gamesdb.launchbox-app.com/games/dbid/{databaseId}/";
        var redirectResponse = downloader.DownloadString(redirectRequestUrl, getContent: false, maxRedirectDepth: -1); //don't accept redirects, just get the URL
        return redirectResponse.ResponseUrl;
    }

    public IEnumerable<LaunchBoxImageDetails> GetGameImageDetails(string detailsUrl)
    {
        var response = downloader.DownloadString(detailsUrl);
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            yield break;

        var doc = new HtmlParser().Parse(response.ResponseContent);
        var imageElements = doc.QuerySelectorAll("article>h3~div img");
        var scriptData = doc.QuerySelector("script#__NUXT_DATA__").TextContent;
        var gameTitle = doc.QuerySelector("h1").TextContent;

        foreach (var img in imageElements)
        {
            var imgDetails = new LaunchBoxImageDetails { ThumbnailUrl = img.GetAttribute("src") };

            var thumbnailFilename = imgDetails.ThumbnailUrl.Split('/').Last();
            var fullImageFilename = FindNextImageFileInNuxtData(scriptData, thumbnailFilename);
            if (fullImageFilename == null)
                continue;

            imgDetails.Url = imgDetails.ThumbnailUrl.Replace(thumbnailFilename, fullImageFilename);

            var alt = img.GetAttribute("alt");
            var altMatch = imgAltRegex.Match(alt, gameTitle.Length + 3); // skip the game title and the connecting " - "
            if(!altMatch.Success)
                continue;

            imgDetails.Type = altMatch.Groups["type"].Value;
            imgDetails.Region = altMatch.Groups["region"].Value;
            imgDetails.Width = int.Parse(altMatch.Groups["width"].Value);
            imgDetails.Height = int.Parse(altMatch.Groups["height"].Value);

            if (imgDetails.Region == "null")
                imgDetails.Region = null;

            yield return imgDetails;
        }
    }

    private readonly Regex imageFilenameRegex = new(@"\b[\w-]+\.[a-z]{3,5}\b", RegexOptions.Compiled);
    private readonly Regex imgAltRegex = new(@"(?<type>.+) \((?<region>[^)]+)\) - (?<width>[0-9]+)x(?<height>[0-9]+)$", RegexOptions.Compiled);

    private string FindNextImageFileInNuxtData(string nuxtData, string thumbFilename)
    {
        var thumbIndex = nuxtData.IndexOf(thumbFilename);
        if (thumbIndex < 0)
            return null;

        var match = imageFilenameRegex.Match(nuxtData, thumbIndex + thumbFilename.Length);
        return match.Success ? match.Value : null;
    }
}

public class LaunchBoxImageDetails
{
    public string Url { get; set; }
    public string ThumbnailUrl { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Type { get; set; }
    public string Region { get; set; }
}