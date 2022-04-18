using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Barnite.Scrapers
{
    public class UpcItemDbScraper : MetadataScraper
    {
        public UpcItemDbScraper(IPlatformUtility platformUtility, IWebDownloader webclient) : base(platformUtility, webclient)
        {
        }

        public override string Name { get; } = "UPCitemdb";

        protected override string GetSearchUrlFromBarcode(string barcode)
        {
            return $"https://api.upcitemdb.com/prod/trial/lookup?upc={barcode}";
        }

        protected override GameMetadata ScrapeGameDetailsHtml(string html)
        {
            var response = Newtonsoft.Json.JsonConvert.DeserializeObject<ApiResponse>(html);
            if (response.Items.Count != 1)
                return null;


            var item = response.Items[0];
            GameMetadata data = new GameMetadata { Description = item.Description, Platforms = new HashSet<MetadataProperty>() };

            data.Name = Regex.Replace(item.Title, @"(\s*(\((?<platform>[a-z 0-9]+)\)|sealed|used|new))+$", (match) =>
            {
                string potentialPlatformName = match.Groups["platform"]?.Value;
                if (string.IsNullOrEmpty(potentialPlatformName))
                    return string.Empty; //remove sealed,new,empty from the end of strings

                var platform = PlatformUtility.GetPlatform(potentialPlatformName);
                if (platform == null)
                {
                    return match.Value;
                }
                else
                {
                    data.Platforms.Add(platform);
                    return string.Empty; //remove platform name from game name
                }
            }, RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

            foreach (var platformName in PlatformUtility.GetPlatformNames())
            {
                if (data.Name.EndsWith(platformName, StringComparison.InvariantCultureIgnoreCase))
                {
                    data.Name = data.Name.TrimEnd(platformName).Trim();
                    data.Platforms.Add(PlatformUtility.GetPlatform(platformName));
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
            public List<Item> Items = new List<Item>();
        }

        private class Item
        {
            public string Title;
            public string Description;
            public List<string> Images = new List<string>();
        }
    }
}
