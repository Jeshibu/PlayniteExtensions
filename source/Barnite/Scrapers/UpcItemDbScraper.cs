using Newtonsoft.Json;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace Barnite.Scrapers;

public class UpcItemDbScraper : MetadataScraper
{
    public override string Name { get; } = "UPCitemdb";
    public override string WebsiteUrl { get; } = "https://www.upcitemdb.com";

    protected override string GetSearchUrlFromBarcode(string barcode)
    {
        return "https://api.upcitemdb.com/prod/trial/lookup?upc=" + HttpUtility.UrlEncode(barcode);
    }

    protected override GameMetadata ScrapeGameDetailsHtml(string html)
    {
        var response = JsonConvert.DeserializeObject<ApiResponse>(html);
        if (response.Items.Count != 1)
            return null;


        var item = response.Items[0];
        GameMetadata data = new() { Description = item.Description, Platforms = [] };

        data.Name = Regex.Replace(item.Title, @"(\s*(\((?<platform>[a-z 0-9]+)\)|\bsealed|\bused|\bnew)\.?)+$", (match) =>
        {
            string potentialPlatformName = match.Groups["platform"]?.Value;
            if (string.IsNullOrEmpty(potentialPlatformName))
                return string.Empty; //remove sealed,new,empty from the end of strings

            var platforms = PlatformUtility.GetPlatforms(potentialPlatformName, strict: true).ToList();
            if (platforms.Count == 0)
            {
                return match.Value;
            }
            else
            {
                foreach (var platform in platforms)
                {
                    data.Platforms.Add(platform);
                }
                return string.Empty; //remove platform name from game name
            }
        }, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

        foreach (var platformName in PlatformUtility.GetPlatformNames())
        {
            if (data.Name.EndsWith(platformName, StringComparison.InvariantCultureIgnoreCase))
            {
                data.Name = data.Name.TrimEnd(platformName).Trim();
                foreach (var platform in PlatformUtility.GetPlatforms(platformName))
                {
                    data.Platforms.Add(platform);
                }
            }
        }

        if (item.Images.Count != 0)
            data.CoverImage = new MetadataFile(item.Images[0]);

        return data;
    }

    protected override IEnumerable<GameLink> ScrapeSearchResultHtml(string html)
    {
        yield break;
    }

    private class ApiResponse
    {
        public string Code;
        public int Total;
        public int Offset;
        public List<Item> Items = [];
    }

    private class Item
    {
        public string Title;
        public string Description;
        public List<string> Images = [];
    }
}
