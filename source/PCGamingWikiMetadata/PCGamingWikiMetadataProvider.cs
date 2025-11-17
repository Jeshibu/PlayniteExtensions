using Playnite.SDK.Plugins;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System;

namespace PCGamingWikiMetadata;

public class PCGamingWikiMetadataProvider : OnDemandMetadataProvider
{
    private readonly MetadataRequestOptions options;
    private readonly IPlayniteAPI playniteApi;
    private readonly PCGamingWikiMetadataSettings settings;

    private readonly PCGWClient client;

    private readonly PCGWGameController gameController;
    private static readonly ILogger logger = LogManager.GetLogger();

    public override List<MetadataField> AvailableFields => field ??= GetAvailableFields();

    private List<MetadataField> GetAvailableFields()
    {
        if (this.gameController.Game == null)
        {
            GetPCGWMetadata();
        }

        var fields = new List<MetadataField>
        {
            MetadataField.Name,
            MetadataField.Links,
            MetadataField.ReleaseDate,
            MetadataField.Genres,
            MetadataField.Series,
            MetadataField.Features,
            MetadataField.Developers,
            MetadataField.Publishers,
            MetadataField.CriticScore,
            MetadataField.Tags
        };

        return fields;
    }

    private void GetPCGWMetadata()
    {
        logger.Debug("GetPCGWMetadata");

        if (this.gameController.Game != null)
        {
            return;
        }

        if (!options.IsBackgroundDownload)
        {
            logger.Debug("not background");
            var item = playniteApi.Dialogs.ChooseItemWithSearch(null, (a) =>
            {
                return client.SearchGames(a);
            }, options.GameData.Name);

            if (item != null)
            {
                var searchItem = item as PCGWGame;
                this.gameController.Game = (PCGWGame)item;
                this.client.FetchGamePageContent(this.gameController.Game);
            }
            else
            {
                this.gameController.Game = new PCGWGame(settings);
                logger.Warn($"Cancelled search");
            }
        }
        else
        {
            try
            {
                List<GenericItemOption> results = client.SearchGames(options.GameData.Name);

                if (results.Count == 0)
                {
                    this.gameController.Game = new PCGWGame(settings);
                    return;
                }

                if (results.Count > 1)
                {
                    logger.Warn($"More than one result for {options.GameData.Name}. Using first result.");
                }

                this.gameController.Game = (PCGWGame)results[0];
                this.client.FetchGamePageContent(this.gameController.Game);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to get PCGW metadata.");
            }
        }
    }

    public PCGamingWikiMetadataProvider(MetadataRequestOptions options, IPlayniteAPI playniteApi, PCGamingWikiMetadataSettings settings)
    {
        this.options = options;
        this.playniteApi = playniteApi;
        this.settings = settings;
        this.gameController = new PCGWGameController(settings);
        this.client = new PCGWClient(this.options, this.gameController);
    }

    public override string GetName(GetMetadataFieldArgs args)
    {
        if (AvailableFields.Contains(MetadataField.Name))
        {
            return this.gameController.Game.Name;
        }

        return base.GetName(args);
    }


    public override IEnumerable<Link> GetLinks(GetMetadataFieldArgs args)
    {
        if (AvailableFields.Contains(MetadataField.Links))
        {
            var links = new List<Link>();
            if (this.gameController.Game.PageID != 0)
                links.Add(this.gameController.Game.PCGamingWikiLink());

            links.AddRange(this.gameController.Game.Links);
            return links;
        }

        return base.GetLinks(args);
    }

    public override ReleaseDate? GetReleaseDate(GetMetadataFieldArgs args)
    {
        if (AvailableFields.Contains(MetadataField.ReleaseDate))
        {
            return this.gameController.Game.WindowsReleaseDate();
        }

        return base.GetReleaseDate(args);
    }

    public override IEnumerable<MetadataProperty> GetGenres(GetMetadataFieldArgs args)
    {

        if (AvailableFields.Contains(MetadataField.Genres))
        {
            return this.gameController.Game.Genres;
        }

        return base.GetGenres(args);
    }

    public override IEnumerable<MetadataProperty> GetFeatures(GetMetadataFieldArgs args)
    {
        if (AvailableFields.Contains(MetadataField.Features))
        {
            return this.gameController.Game.Features;
        }

        return base.GetFeatures(args);
    }

    public override IEnumerable<MetadataProperty> GetSeries(GetMetadataFieldArgs args)
    {
        if (AvailableFields.Contains(MetadataField.Series))
        {
            return this.gameController.Game.Series;
        }

        return base.GetSeries(args);
    }

    public override IEnumerable<MetadataProperty> GetDevelopers(GetMetadataFieldArgs args)
    {
        if (AvailableFields.Contains(MetadataField.Developers))
        {
            return this.gameController.Game.Developers;
        }

        return base.GetDevelopers(args);
    }

    public override int? GetCriticScore(GetMetadataFieldArgs args)
    {

        if (AvailableFields.Contains(MetadataField.CriticScore) &&
                (this.gameController.Game.GetOpenCriticReception(out int? score) ||
                this.gameController.Game.GetIGDBReception(out score) ||
                this.gameController.Game.GetMetacriticReception(out score))
            )
        {
            return score;
        }

        return base.GetCriticScore(args);
    }

    public override IEnumerable<MetadataProperty> GetPublishers(GetMetadataFieldArgs args)
    {
        if (AvailableFields.Contains(MetadataField.Publishers))
        {
            return this.gameController.Game.Publishers;
        }

        return base.GetPublishers(args);
    }

    public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
    {
        if (AvailableFields.Contains(MetadataField.Tags))
        {
            return this.gameController.Game.Tags;
        }

        return base.GetTags(args);
    }
}
