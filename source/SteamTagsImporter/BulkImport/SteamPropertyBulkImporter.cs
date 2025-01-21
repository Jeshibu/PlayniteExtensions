using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SteamTagsImporter.BulkImport
{
    public class SteamPropertyBulkImporter : BulkGamePropertyAssigner<SteamProperty, GamePropertyImportViewModel>
    {
        public override string MetadataProviderName { get; } = "Steam";
        private readonly SteamTagsImporterSettings settings;
        private readonly SteamIdUtility steamIdUtility;

        public SteamPropertyBulkImporter(IPlayniteAPI playniteAPI, ISearchableDataSourceWithDetails<SteamProperty, IEnumerable<GameDetails>> dataSource, IPlatformUtility platformUtility, SteamTagsImporterSettings settings)
            : base(playniteAPI, dataSource, platformUtility, new SteamIdUtility(), ExternalDatabase.Steam, settings.MaxDegreeOfParallelism)
        {
            AllowEmptySearchQuery = true;
            this.settings = settings;
            this.steamIdUtility = (SteamIdUtility)DatabaseIdUtility;
        }

        protected override string GetGameIdFromUrl(string url)
        {
            var dbId = DatabaseIdUtility.GetIdFromUrl(url);
            if (dbId.Database == ExternalDatabase.Steam)
                return dbId.Id;

            return null;
        }

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

        private bool ContainsUrl(IEnumerable<Link> links, string url)
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

        private string StripUrl(string steamUrl)
        {
            if (string.IsNullOrWhiteSpace(steamUrl))
                return steamUrl;

            return StripSlugIfStoreUrl(steamUrl).TrimStart("steam://openurl/").TrimEnd("/");
        }

        private string StripSlugIfStoreUrl(string steamUrl)
        {
            var match = steamIdUtility.SteamUrlRegex.Match(steamUrl);
            if (!match.Success || !steamUrl.Contains("store.steampowered.com"))
                return steamUrl;

            return match.Value;
        }

        protected override IEnumerable<CheckboxFilter> GetCheckboxFilters(GamePropertyImportViewModel viewModel)
        {
            foreach (var f in base.GetCheckboxFilters(viewModel))
                yield return f;

            yield return new CheckboxFilter("Only Steam games", viewModel, c => steamIdUtility.LibraryIds.Contains(c.Game.PluginId));
        }
    }
}
