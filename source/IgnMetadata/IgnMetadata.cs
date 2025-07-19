using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;

namespace IgnMetadata;

public class IgnMetadata : MetadataPlugin
{
    private static readonly ILogger logger = LogManager.GetLogger();

    private IPlatformUtility platformUtility;
    private IgnClient client;

    public override Guid Id { get; } = Guid.Parse("6024e3a9-de7e-4848-9101-7a2f818e7e47");

    public override List<MetadataField> SupportedFields => Fields;

    internal static List<MetadataField> Fields =
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
    ];

    public override string Name => "IGN";

    public IgnMetadata(IPlayniteAPI api) : base(api)
    {
        Properties = new MetadataPluginProperties
        {
            HasSettings = false
        };
        platformUtility = new PlatformUtility(PlayniteApi);
        client = new IgnClient(new WebDownloader() { Accept = "*/*" });
    }

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        var searchProvider = new IgnGameSearchProvider(client, platformUtility);
        return new IgnMetadataProvider(searchProvider, options, this.PlayniteApi, platformUtility);
    }
}