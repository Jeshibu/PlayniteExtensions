﻿using System.Collections.Generic;
using System.Text.RegularExpressions;
using AngleSharp.Parser.Html;
using PlayniteExtensions.Common;

namespace LaunchBoxMetadata;

public class LaunchBoxWebscraper(IWebDownloader downloader)
{
    public string GetLaunchBoxGamesDatabaseUrl(string databaseId)
    {
        var redirectRequestUrl = $"https://gamesdb.launchbox-app.com/games/dbid/{databaseId}/";
        var redirectResponse = downloader.DownloadString(redirectRequestUrl, getContent: false, maxRedirectDepth: -1); //don't accept redirects, just get the URL
        return redirectResponse.ResponseUrl;
    }

    public IEnumerable<LaunchBoxImageDetails> GetGameImageDetails(string detailsUrl)
    {
        var imageUrl = detailsUrl.Replace("/details/", "/images/");

        var response = downloader.DownloadString(imageUrl);
        if (response.StatusCode != System.Net.HttpStatusCode.OK)
            yield break;

        var parser = new HtmlParser();
        var doc = parser.Parse(response.ResponseContent);
        var gameTitle = doc.QuerySelector("div.heading > h1").TextContent;
        var imageLinks = doc.QuerySelectorAll("a[data-gameimagekey]");
        foreach (var l in imageLinks)
        {
            var imgDetails = new LaunchBoxImageDetails();
            imgDetails.Url = l.GetAttribute("href");

            var footer = l.GetAttribute("data-footer");
            var footerMatch = ImageSizeRegex.Match(footer);
            if (footerMatch.Success)
            {
                imgDetails.Width = int.Parse(footerMatch.Groups["width"].Value);
                imgDetails.Height = int.Parse(footerMatch.Groups["height"].Value);
            }

            var dataTitle = l.GetAttribute("data-title");
            imgDetails.Type = GetImageType(dataTitle, gameTitle, out string region);
            imgDetails.Region = region;

            var imgElement = l.QuerySelector("img");
            imgDetails.ThumbnailUrl = imgElement.GetAttribute("src");
            yield return imgDetails;
        }
    }

    private string GetImageType(string title, string gameTitle, out string region)
    {
        var gameTitleRemoved = title.Remove(0, (gameTitle + " - ").Length);
        string reg = null;
        var type = RegionRegex.Replace(gameTitleRemoved, match =>
        {
            if (match.Groups["region"].Success)
                reg = match.Groups["region"].Value;

            return string.Empty;
        });
        region = reg;
        return type;
    }

    private static Regex ImageSizeRegex = new(@"(?<width>\d+) x (?<height>\d+) (?<filetype>[A-Z0-9]+)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    private static Regex RegionRegex = new(@"(\s+(Image|\((?<region>[\w\s]+)\))){1,2}$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
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