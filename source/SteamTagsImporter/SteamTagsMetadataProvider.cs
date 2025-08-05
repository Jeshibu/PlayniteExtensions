using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System.Collections.Generic;
using System.Linq;

namespace SteamTagsImporter;

public class SteamTagsMetadataProvider(SteamTagsGetter tagsGetter, MetadataRequestOptions options, Plugin plugin) : OnDemandMetadataProvider
{
    public override List<MetadataField> AvailableFields { get; } = [MetadataField.Tags];

    public override IEnumerable<MetadataProperty> GetTags(GetMetadataFieldArgs args)
    {
        var steamTags = tagsGetter.GetSteamTags(options.GameData, out bool newTagsAddedToSettings);
        if (newTagsAddedToSettings)
            plugin.SavePluginSettings(tagsGetter.Settings);

        var tagNames = steamTags.Select(t => tagsGetter.GetFinalTagName(t.Name)).ToList();
        return tagNames.NullIfEmpty()?.Select(tn => new MetadataNameProperty(tn));
    }
}