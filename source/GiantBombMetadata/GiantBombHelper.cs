using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GiantBombMetadata
{
    public static class GiantBombHelper
    {
        private static Regex gameIdRegex = new Regex(@"\bgiantbomb\.com/.+(?<guid>\b3030-[0-9]+\b)", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static string GetGiantBombGuidFromGameLinks(this Game game)
        {
            if (game?.Links == null)
                return null;

            foreach (var link in game.Links)
            {
                var guid = GetGiantBombGuidFromUrl(link.Url);
                if (guid != null)
                    return guid;
            }
            return null;
        }

        public static string GetGiantBombGuidFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return null;

            var match = gameIdRegex.Match(url);
            if (match.Success)
                return match.Groups["guid"].Value;
            else
                return null;
        }

        public static string MakeHtmlUrlsAbsolute(string htmlContent, string baseUrl) => HtmlAgilityPackExtensions.MakeHtmlUrlsAbsolute(htmlContent, baseUrl);
    }

    public class GiantBombIdUtility : SingleExternalDatabaseIdUtility
    {
        public override ExternalDatabase Database { get; } = ExternalDatabase.GiantBomb;

        public override IEnumerable<Guid> LibraryIds { get; } = new Guid[0];

        public override (ExternalDatabase Database, string Id) GetIdFromUrl(string url)
        {
            var id = GiantBombHelper.GetGiantBombGuidFromUrl(url);
            if (id == null)
                return (ExternalDatabase.None, null);
            else
                return (ExternalDatabase.GiantBomb, id);
        }
    }
}
