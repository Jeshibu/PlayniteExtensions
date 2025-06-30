using MobyGamesMetadata.Api;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;

namespace MobyGamesMetadata;

public class MobyGamesMetadataProvider : GenericMetadataProvider<GameSearchResult>
{
    private readonly MobyGamesMetadata plugin;
    private readonly MobyGamesMetadataSettings settings;

    public override List<MetadataField> AvailableFields => plugin.SupportedFields;

    protected override string ProviderName { get; } = "MobyGames";

    public MobyGamesMetadataProvider(MetadataRequestOptions options, MobyGamesMetadata plugin, IGameSearchProvider<GameSearchResult> dataSource, IPlatformUtility platformUtility, MobyGamesMetadataSettings settings)
        :base(dataSource, options, plugin.PlayniteApi, platformUtility)
    {
        this.plugin = plugin;
        this.settings = settings;
    }

    protected override bool FilterImage(GameField field, IImageData imageData)
    {
        MobyGamesImageSourceSettings imgSettings;

        switch (field)
        {
            case GameField.CoverImage:
                 imgSettings = settings.Cover;
                break;
            case GameField.BackgroundImage:
                imgSettings = settings.Background;
                break;
            default:
                return true;
        }

        if (imageData.Width < imgSettings.MinWidth || imageData.Height < imgSettings.MinHeight)
            return false;

        switch (imgSettings.AspectRatio)
        {
            case AspectRatio.Vertical:
                return imageData.Width < imageData.Height;
            case AspectRatio.Horizontal:
                return imageData.Width > imageData.Height;
            case AspectRatio.Square:
                return imageData.Width == imageData.Height;
            case AspectRatio.Any:
            default:
                return true;
        }
    }

    public override void Dispose()
    {
        if(dataSource is IDisposable disposableDataSource)
            disposableDataSource.Dispose();
        
        base.Dispose();
    }
}
