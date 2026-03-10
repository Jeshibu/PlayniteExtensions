using System;
using System.Collections.Generic;
using IgnMetadata.Api;
using IgnMetadata.HowLongToBeat;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System.Linq;

namespace IgnMetadata;

public class IgnMetadata : MetadataPlugin
{
    private static readonly ILogger logger = LogManager.GetLogger();

    private readonly IPlatformUtility platformUtility;
    private IWebDownloader Downloader => field ??= new WebDownloader { Accept = "*/*" };
    private bool HowLongToBeatIsInstalled { get; set; }

    public override Guid Id { get; } = Guid.Parse("6024e3a9-de7e-4848-9101-7a2f818e7e47");

    public override List<MetadataField> SupportedFields => Fields;

    internal static readonly List<MetadataField> Fields =
    [
        MetadataField.CoverImage,
        MetadataField.Name,
        MetadataField.Developers,
        MetadataField.Publishers,
        MetadataField.Genres,
        MetadataField.Features,
        MetadataField.Series,
        MetadataField.Description,
        MetadataField.AgeRating,
        MetadataField.ReleaseDate,
        MetadataField.Platform,
        MetadataField.Links,
        MetadataField.BackgroundImage,
    ];

    public override string Name => "IGN";

    public IgnMetadata(IPlayniteAPI api) : base(api)
    {
        Properties = new MetadataPluginProperties
        {
            HasSettings = false
        };
        platformUtility = new PlatformUtility(PlayniteApi);
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        var searchProvider = new IgnGameSearchProvider(new IgnApiClient(Downloader), platformUtility);
        return new IgnMetadataProvider(searchProvider, options, PlayniteApi, platformUtility);
    }

    public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
    {
        HowLongToBeatIsInstalled = PlayniteApi.Addons.Addons.Contains("playnite-howlongtobeat-plugin");
    }

    public override IEnumerable<GameMenuItem> GetGameMenuItems(GetGameMenuItemsArgs args)
    {
        if (!HowLongToBeatIsInstalled)
            yield break;

        var writer = new HltbDataWriter(PlayniteApi.Paths.ExtensionsDataPath);
        if (!args.Games.Any(writer.HasNoHltbData))
            yield break;

        yield return new() { MenuSection = "HowLongToBeat", Description = "Set data from IGN", Action = x => ImportHowLongToBeatData(x.Games, showResultDialog: true) };
    }

    private void ImportHowLongToBeatData(List<Game> games, bool showResultDialog)
    {
        var setter = new MassHltbDataSetter(PlayniteApi, Downloader, showResultDialog);
        setter.SetHltbData(games);
    }
}
