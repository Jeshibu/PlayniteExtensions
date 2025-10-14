using EaLibrary.Models;
using EaLibrary.Services;
using Microsoft.Win32;
using Newtonsoft.Json;
using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EaLibrary;

public class EaLibraryDataGatherer(IEaWebsite website, IRegistryValueProvider registry, IPlatformUtility platformUtility, string pluginUserDataPath)
{
    private readonly string _legacyOfferCacheFilePath = Path.Combine(pluginUserDataPath, "legacy-offers.json");
    private readonly string[] _realOwnershipMethods = ["UNKNOWN", "ASSOCIATION", "PURCHASE", "REDEMPTION", "GIFT_RECEIPT", "ENTITLEMENT_GRANT", "DIRECT_ENTITLEMENT", "PRE_ORDER_PURCHASE", "STEAM", "EPIC"];
    private readonly string[] _eaPlayOwnershipMethods = ["VAULT", "STEAM_VAULT", "STEAM_SUBSCRIPTION", "EPIC_VAULT", "EPIC_SUBSCRIPTION"];
    private const string XboxGamePassOwnershipMethod = "XGP_VAULT";
    private readonly ILogger _logger = LogManager.GetLogger();

    public IEnumerable<GameMetadata> GetGames()
    {
        var token = website.GetAuthToken();
        if (token == null) throw new AuthenticationException();
        var ownedGames = website.GetOwnedGames(token);
        var legacyOffers = GetLegacyOffers(ownedGames.Select(o => o.originOfferId));

        foreach (var game in ownedGames)
            yield return ToGameMetadata(game, legacyOffers);
    }

    public async Task<LegacyOffer> GetLegacyOfferAsync(string offerId)
    {
        var offers = await GetLegacyOffersAsync([offerId]);
        if (offers.TryGetValue(offerId, out var offer))
            return offer;

        return null;
    }

    public async Task<EaInstallationStatus> GetGameInstallationStatusAsync(string offerId)
    {
        var manifest = await GetLegacyOfferAsync(offerId);

        if (manifest?.installCheckOverride == null)
        {
            _logger.Error($"No install data found for EA game {offerId}, stopping installation check.");
            return new();
        }

        var installData = GetInstallDirectory(manifest.installCheckOverride);

        if (installData == null)
            return new();

        var executablePath = Path.Combine(installData.InstallDirectory, installData.RelativeFilePath);
        return new()
        {
            IsInstalled = File.Exists(executablePath),
            InstallDirectory = installData.InstallDirectory,
            ExePath = executablePath,
        };
    }

    private Dictionary<string, LegacyOffer> GetLegacyOffers(IEnumerable<string> offerIds) => GetLegacyOffersAsync(offerIds).Result;

    private async Task<Dictionary<string, LegacyOffer>> GetLegacyOffersAsync(IEnumerable<string> offerIds)
    {
        var offers = GetCachedLegacyOffers();

        var missingOfferIds = offerIds.Except(offers.Keys).ToArray();

        if (missingOfferIds.Any())
        {
            var missingOffers = await website.GetLegacyOffersAsync(missingOfferIds);
            foreach (var offer in missingOffers)
                offers[offer.offerId] = offer;

            var serialized = JsonConvert.SerializeObject(offers, Formatting.Indented);
            File.WriteAllText(_legacyOfferCacheFilePath, serialized);
        }

        return offers;
    }

    private Dictionary<string, LegacyOffer> GetCachedLegacyOffers()
    {
        if (!File.Exists(_legacyOfferCacheFilePath))
            return new();

        var strContent = File.ReadAllText(_legacyOfferCacheFilePath);
        return JsonConvert.DeserializeObject<Dictionary<string, LegacyOffer>>(strContent);
    }

    private GameMetadata ToGameMetadata(OwnedGameProduct game, IDictionary<string, LegacyOffer> offers)
    {
        var output = new GameMetadata
        {
            GameId = game.originOfferId,
            Name = game.product.name.RemoveTrademarks(),
            Platforms = GetPlatforms(game),
            Source = GetGameSource(game),
        };

        if (offers.TryGetValue(game.originOfferId, out var offer))
        {
            var install = GetInstallDirectory(offer.installCheckOverride);
            if (install != null)
            {
                output.InstallDirectory = install.InstallDirectory;
                var installCheckFile = Path.Combine(install.InstallDirectory, install.RelativeFilePath);
                output.IsInstalled = File.Exists(installCheckFile);
            }
        }

        return output;
    }

    private MetadataNameProperty GetGameSource(OwnedGameProduct game)
    {
        var ownershipMethods = game.product.gameProductUser.ownershipMethods;
        if (ownershipMethods.IntersectsPartiallyWith(_realOwnershipMethods))
            return new("EA app");

        if (ownershipMethods.Contains(XboxGamePassOwnershipMethod))
            return new("Xbox Game Pass");

        if (ownershipMethods.IntersectsPartiallyWith(_eaPlayOwnershipMethods))
            return new("EA Play");

        _logger.Warn($"Unknown ownership methods: {string.Join(", ", ownershipMethods)}");
        return new("EA app");
    }

    private HashSet<MetadataProperty> GetPlatforms(OwnedGameProduct game)
    {
        var splitPlatforms = game.product.gamePlatformDetails.gamePlatform.Split('_');
        return splitPlatforms.SelectMany(platformUtility.GetPlatforms).ToHashSet();
    }

    private static readonly Regex installDirRegex = new(@"^\[(?<reg>.+?)\](?<exe>.*)$", RegexOptions.Compiled);

    internal InstallPath GetInstallDirectory(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        var match = installDirRegex.Match(raw);
        if (!match.Success)
            return null;

        var registryPath = match.Groups["reg"].Value;
        var regSplit = registryPath.Split('\\');

        var hive = regSplit[0] switch
        {
            "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKEY_USERS" => RegistryHive.Users,
            "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => throw new ArgumentOutOfRangeException(regSplit[0]),
        };
        var path = string.Join(@"\", regSplit.Skip(1).Take(regSplit.Length - 2));
        var keyName = regSplit.Last();
        var installDirectory = registry.GetValueForPath(hive, path, keyName);

        if (string.IsNullOrWhiteSpace(installDirectory))
        {
            _logger.Info($"Install directory not found in registry: {registryPath}");
            return null;
        }

        return new(installDirectory, match.Groups["exe"].Value.TrimStart('\\', '/'));
    }

    public class InstallPath(string installDirectory, string relativePath)
    {
        public string InstallDirectory { get; } = installDirectory;
        public string RelativeFilePath { get; } = relativePath;
    }
}
