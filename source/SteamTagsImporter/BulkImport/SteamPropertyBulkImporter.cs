using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;

namespace SteamTagsImporter.BulkImport
{
    public class SteamPropertyBulkImporter : BulkGamePropertyAssigner<SteamProperty, GamePropertyImportViewModel>
    {
        public override string MetadataProviderName => "Steam";
        private readonly SteamTagsImporterSettings settings;

        public SteamPropertyBulkImporter(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<SteamProperty, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, SteamTagsImporterSettings settings)
            : base(playniteAPI, dataSource, platformUtility, settings.MaxDegreeOfParallelism)
        {
            AllowEmptySearchQuery = true;
            this.settings = settings;
        }

        protected override string GetGameIdFromUrl(string url) => SteamAppIdUtility.GetSteamGameIdFromUrl(url);

        protected override PropertyImportSetting GetPropertyImportSetting(SteamProperty searchItem, out string name)
        {
            name = searchItem.Name;

            var target = GetTarget(searchItem.Param);

            return new PropertyImportSetting
            {
                ImportTarget = target,
                Prefix = (settings.UseTagPrefix && target == PropertyImportTarget.Tags) ? settings.TagPrefix : null
            };
        }

        protected override string GetIdFromGameLibrary(Guid libraryPluginId, string gameId) => libraryPluginId == SteamAppIdUtility.SteamLibraryPluginId ? gameId : null;

        private static PropertyImportTarget GetTarget(string param)
        {
            switch (param)
            {
                case "tags": return PropertyImportTarget.Tags;
                default: return PropertyImportTarget.Features;
            }
        }

        protected override IEnumerable<PotentialLink> GetPotentialLinks(SteamProperty searchItem)
        {
            yield return new PotentialLink(MetadataProviderName, game => game.Url, ContainsUrl);
            yield return new PotentialLink("Discussions", game => $"https://steamcommunity.com/app/{game.Id}/discussions/", ContainsUrl) { Checked = false };
            yield return new PotentialLink("Guides", game => $"https://steamcommunity.com/app/{game.Id}/guides/", ContainsUrl) { Checked = false };

            if (searchItem.Param != "category2")
                yield break;

            switch (searchItem.Value)
            {
                case "22": //Achievements
                    yield return new PotentialLink("Achievements", game => $"https://steamcommunity.com/stats/{game.Id}/achievements", ContainsUrl);
                    break;
                case "29": //Cards
                    yield return new PotentialLink("Badge Progress", game => $"https://steamcommunity.com/my/gamecards/{game.Id}", ContainsUrl);
                    yield return new PotentialLink("Points Shop", game => $"https://store.steampowered.com/points/shop/app/{game.Id}/", ContainsUrl);
                    break;
                case "30": //Workshop
                    yield return new PotentialLink("Workshop", game => $"https://steamcommunity.com/app/{game.Id}/workshop/", ContainsUrl);
                    break;
            }
        }

        private static bool ContainsUrl(IEnumerable<Link> links, string url)
        {
            if (links == null)
                return false;

            var strippedUrl = StripUrl(url);

            foreach (var link in links)
            {
                var slu = StripUrl(link.Url);
                if (strippedUrl.Equals(slu, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string StripUrl(string steamUrl)
        {
            if (string.IsNullOrWhiteSpace(steamUrl))
                return steamUrl;

            return StripSlugIfStoreUrl(steamUrl).TrimStart("steam://openurl/").TrimEnd("/");
        }

        private static string StripSlugIfStoreUrl(string steamUrl)
        {
            var match = SteamAppIdUtility.SteamUrlRegex.Match(steamUrl);
            if (!match.Success || !steamUrl.Contains("store.steampowered.com"))
                return steamUrl;

            return match.Value;
        }

        protected override IEnumerable<CheckboxFilter> GetCheckboxFilters(GamePropertyImportViewModel viewModel)
        {
            foreach (var f in base.GetCheckboxFilters(viewModel))
                yield return f;

            yield return new CheckboxFilter("Only Steam games", viewModel, c => c.Game.PluginId == SteamAppIdUtility.SteamLibraryPluginId);
        }
    }
}
