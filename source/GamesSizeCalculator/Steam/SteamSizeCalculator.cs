﻿using GamesSizeCalculator.Models;
using Playnite.SDK;
using Playnite.SDK.Models;
using SteamKit2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GamesSizeCalculator.SteamSizeCalculation;

public class SteamSizeCalculator(ISteamApiClient steamApiClient, ISteamAppIdUtility steamAppIdUtility, GamesSizeCalculatorSettings settings) : ISizeCalculator
{
    private readonly ILogger logger = LogManager.GetLogger();
    public ISteamApiClient SteamApiClient { get; } = steamApiClient;
    public ISteamAppIdUtility SteamAppIdUtility { get; } = steamAppIdUtility;
    public GamesSizeCalculatorSettings Settings { get; } = settings;
    public string ServiceName { get; } = "Steam";

    public async Task<ulong?> GetInstallSizeAsync(Game game)
    {
        if (!SteamCommon.Steam.IsGameSteamGame(game) && !Settings.GetSizeFromSteamNonSteamGames)
        {
            return null;
        }

        var appIdStr = SteamAppIdUtility.GetSteamGameId(game);
        if (!uint.TryParse(appIdStr, out uint appId))
        {
            return null;
        }

        return await GetInstallSizeAsync(appId);
    }

    private async Task<ulong?> GetInstallSizeAsync(uint appId)
    {
        var depotData = await GetRelevantDepots(appId);
        if (depotData == null)
        {
            return null;
        }

        RemoveRegionalDepots(ref depotData);

        IEnumerable<DepotInfo> filteredDepots = depotData;

        if (!Settings.IncludeDlcInSteamCalculation)
        {
            filteredDepots = filteredDepots.Where(d => !d.IsDLC);
        }

        if (!Settings.IncludeOptionalInSteamCalculation && !depotData.All(d => d.Optional)) //If everything is optional, don't filter out optional depots
        {
            filteredDepots = filteredDepots.Where(d => !d.Optional);
        }

        ulong size = 0UL;
        foreach (var depot in filteredDepots)
        {
            size += depot.FileSize;
        }

        return size;
    }

    private void RemoveRegionalDepots(ref List<DepotInfo> allDepots)
    {
        var parsedDepots = allDepots.Select(ParseDepotName).ToList();
        var blacklistedDepots = parsedDepots.Where(d => Settings.DepotRegionWordsBlacklist.Contains(d.RegionWord, StringComparer.InvariantCultureIgnoreCase)).OrderBy(d => d.Depot.FileSize).ToList();
        foreach (var d in blacklistedDepots)
        {
            if (allDepots.Count(x => !x.IsDLC) == 1 && !d.Depot.IsDLC)
                break;

            logger.Trace($"Removing depot {d.BaseName} due to blacklisted region word: {d.RegionWord}");
            parsedDepots.Remove(d);
            allDepots.Remove(d.Depot);
        }
        var grouped = parsedDepots.GroupBy(d => d.BaseName);

        foreach (var group in grouped)
        {
            var key = group.Key;
            var orderedDepots = group.OrderBy(d => d.Depot.Optional).ThenBy(d => d.RegionWord == null ? -2 : Settings.DepotRegionWords.IndexOf(d.RegionWord)).ToList();
            if (orderedDepots.Count == 1 || string.IsNullOrWhiteSpace(key))
                continue;

            StringBuilder logStringBuilder = new($"Depot group {key}, {orderedDepots.Count} depots: ");
            logStringBuilder.AppendLine();

            for (int i = 0; i < orderedDepots.Count; i++)
            {
                var depotData = orderedDepots[i];
                string depotLogString = $"{depotData.Depot.Id} - {depotData.Depot.Name}, optional: {depotData.Depot.Optional}, dlc: {depotData.Depot.IsDLC}, size: {depotData.Depot.FileSize}, {depotData.BaseName}|{depotData.RegionWord}|{depotData.Rank}";

                if (i == 0)
                {
                    logStringBuilder.AppendLine("Keeping depot: " + depotLogString);
                }
                else
                {
                    logStringBuilder.AppendLine("Removing depot: " + depotLogString);
                    allDepots.Remove(depotData.Depot);
                }
            }
            logger.Debug(logStringBuilder.ToString());
        }
    }

    private DepotGroupingInfo ParseDepotName(DepotInfo depot)
    {
        var baseName = RemoveRegionWords(depot.Name, out string regionWord);
        int rank = string.IsNullOrWhiteSpace(regionWord) ? -2 : Settings.DepotRegionWords.IndexOf(regionWord);
        return new DepotGroupingInfo { BaseName = baseName, RegionWord = regionWord, Depot = depot, Rank = rank };
    }

    private string RemoveRegionWords(string str, out string regionalWord)
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            regionalWord = string.Empty;
            return string.Empty;
        }

        var words = str.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            regionalWord = string.Empty;
            return string.Empty;
        }

        var wordsStack = new Stack<string>(words);
        var lastWord = wordsStack.Pop();
        if (new[] { "content", "depot" }.Contains(lastWord, StringComparer.InvariantCultureIgnoreCase))
        {
            if (wordsStack.Count == 0)
            {
                regionalWord = string.Empty;
                return string.Empty;
            }

            lastWord = wordsStack.Pop();
        }

        if (Settings.DepotRegionWords.Contains(lastWord, StringComparer.InvariantCultureIgnoreCase)
            || Settings.DepotRegionWordsBlacklist.Contains(lastWord, StringComparer.InvariantCultureIgnoreCase))
        {
            regionalWord = lastWord.ToLowerInvariant();
            var baseStringWords = new string[wordsStack.Count];
            wordsStack.CopyTo(baseStringWords, 0);
            return string.Join(" ", baseStringWords.Reverse());
        }
        else
        {
            regionalWord = null;
            return string.Join(" ", words);
        }
    }

    private static string GetLastWord(string str)
    {
        if (str.IsNullOrWhiteSpace())
        {
            return str;
        }

        str = str.Trim();
        var i = str.LastIndexOfAny(new[] { ' ', '_', '-' });
        if (i == -1)
        {
            return str;
        }

        var lastWord = str.Substring(i + 1);
        if (lastWord.Equals("content", StringComparison.InvariantCultureIgnoreCase)
            || lastWord.Equals("depot", StringComparison.InvariantCultureIgnoreCase))
        {
            string retryStr = str.Remove(i).Trim();
            return GetLastWord(retryStr);
        }
        else
        {
            return lastWord;
        }
    }

    private async Task<List<DepotInfo>> GetRelevantDepots(uint appId)
    {
        var productInfo = await SteamApiClient.GetProductInfo(appId);
        var depots = GetKeyValueNode(productInfo, "depots");
        if (depots == null || depots == KeyValue.Invalid)
        {
            logger.Warn($"No depots for {appId}");
            return null;
        }

        var output = new List<DepotInfo>();
        foreach (var depot in depots.Children)
        {
            var id = GetValue(depot);
            var name = GetKeyValueNode(depot, "name")?.Value;

            var maxsizeStr = GetValue(depot, "maxsize");
            var sizeStr = GetValue(depot, "manifests", "public", "size");
            if (!ulong.TryParse(maxsizeStr, out ulong maxsize) && !ulong.TryParse(sizeStr, out maxsize))
            {
                continue;
            }

            var sharedinstallNode = GetValue(depot, "sharedinstall");
            if (sharedinstallNode == "1")
            {
                logger.Debug($"Skipped shared depot {name}");
                continue;
            }

            var language = GetValue(depot, "config", "language");
            if (!language.IsNullOrEmpty() && !"english".Equals(language, StringComparison.InvariantCultureIgnoreCase))
            {
                logger.Debug($"Skipping depot \"{name}\" because its language is \"{language}\"");
                continue;
            }

            var oslist = GetValue(depot, "config", "oslist");
            if (!oslist.IsNullOrEmpty() && !oslist.Contains("windows", StringComparison.InvariantCultureIgnoreCase))
            {
                logger.Debug($"Skipping depot \"{name}\" because its OS list is \"{oslist}\"");
                continue;
            }

            var optional = GetValue(depot, "optional") == "1";

            var dlcAppId = GetValue(depot, "dlcappid");

            logger.Debug($"Depot {name}, size {maxsize} for appId {appId}, dlc app ID: {dlcAppId}");

            output.Add(new DepotInfo(id, name, maxsize, dlcAppId != null, optional));
        }

        return output;
    }

    private KeyValue GetKeyValueNode(KeyValue kv, params string[] names)
    {
        if (kv == null || kv == KeyValue.Invalid)
        {
            return null;
        }

        KeyValue node = kv;
        foreach (var name in names)
        {
            node = node[name];
            if (node == null || node == KeyValue.Invalid)
            {
                return null;
            }
        }
        return node;
    }

    private string GetValue(KeyValue kv, params string[] names)
    {
        return GetKeyValueNode(kv, names)?.Value;
    }

    public bool IsPreferredInstallSizeCalculator(Game game)
    {
        return SteamCommon.Steam.IsGameSteamGame(game);
    }
}