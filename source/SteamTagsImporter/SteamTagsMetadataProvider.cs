using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System.Collections.Generic;
using System.Linq;

namespace SteamTagsImporter;

public class SteamTagsMetadataProvider : OnDemandMetadataProvider
{
    private readonly SteamTagsGetter tagsGetter;
    private readonly MetadataRequestOptions options;
    private readonly Plugin plugin;

    public SteamTagsMetadataProvider(SteamTagsGetter tagsGetter, MetadataRequestOptions options, Plugin plugin)
    {
        this.tagsGetter = tagsGetter;
        this.options = options;
        this.plugin = plugin;
    }

    public override List<MetadataField> AvailableFields { get; } = new List<MetadataField> { MetadataField.Tags };

    public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
    {
        var steamTags = tagsGetter.GetSteamTags(options.GameData, out bool newTagsAddedToSettings);
        if (newTagsAddedToSettings)
            plugin.SavePluginSettings(tagsGetter.Settings);

        var tagNames = steamTags.Select(t => tagsGetter.GetFinalTagName(t.Name)).ToList();
        return tagNames.NullIfEmpty()?.Select(tn => new MetadataNameProperty(tn));
    }
}