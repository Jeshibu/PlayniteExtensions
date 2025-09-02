﻿using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteUtilitiesCommon;
using System;
using System.Collections.Generic;

namespace GamesSizeCalculator;

public class InstallSizeProvider(Game game, IPlayniteAPI playniteAPI, ICollection<ISizeCalculator> sizeCalculators) : OnDemandMetadataProvider
{
    public override List<MetadataField> AvailableFields { get; } = [MetadataField.InstallSize];
    private Game Game { get; } = game;
    private IPlayniteAPI PlayniteApi { get; } = playniteAPI;
    private ICollection<ISizeCalculator> SizeCalculators { get; } = sizeCalculators;
    private readonly ILogger logger = LogManager.GetLogger();

    public override ulong? GetInstallSize(GetMetadataFieldArgs args)
    {
        ulong size = GetInstallSize();
        return size == 0 ? null : size;
    }

    private ulong GetInstallSize(ISizeCalculator sizeCalculator)
    {
        try
        {
            var sizeTask = sizeCalculator.GetInstallSizeAsync(Game);
            if (sizeTask.Wait(7000))
                return sizeTask.Result ?? 0L;

            logger.Warn($"Timed out while getting {sizeCalculator.ServiceName} install size for {Game.Name}");
            return 0L;
        }
        catch (Exception e)
        {
            logger.Error(e, $"Error while getting file size from {sizeCalculator?.ServiceName} for {Game?.Name}");
            PlayniteApi.Notifications.Add(
                new NotificationMessage("GetOnlineSizeError" + Game?.Id,
                    string.Format(ResourceProvider.GetString("LOCGame_Sizes_Calculator_NotificationMessageErrorGetOnlineSize"), sizeCalculator.ServiceName, Game.Name, e.Message),
                    NotificationType.Error));
            return 0;
        }
    }

    private ulong GetInstallSize()
    {
        if (!(SizeCalculators?.Count > 0 && (Game.IsInstalled || PlayniteUtilities.IsGamePcGame(Game))))
        {
            return 0;
        }

        ulong size = 0;

        var alreadyRan = new List<ISizeCalculator>();
        //check the preferred size calculators first (Steam for Steam games, GOG for GOG games, etc)
        foreach (var sizeCalculator in SizeCalculators)
        {
            if (!sizeCalculator.IsPreferredInstallSizeCalculator(Game))
            {
                continue;
            }

            size = GetInstallSize(sizeCalculator);
            alreadyRan.Add(sizeCalculator);
            if (size != 0)
            {
                break;
            }
        }

        //go through every size calculator as a fallback
        if (size == 0)
        {
            foreach (var sizeCalculator in SizeCalculators)
            {
                if (alreadyRan.Contains(sizeCalculator))
                {
                    continue;
                }

                size = GetInstallSize(sizeCalculator);
                if (size != 0)
                {
                    break;
                }
            }
        }

        return size;
    }

}