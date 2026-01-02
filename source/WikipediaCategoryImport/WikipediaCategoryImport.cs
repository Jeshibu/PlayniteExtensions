using JetBrains.Annotations;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;

namespace WikipediaCategoryImport;

[UsedImplicitly]
public class WikipediaCategoryImport(IPlayniteAPI playniteApi) : MetadataPlugin(playniteApi)
{
    public override Guid Id { get; } = Guid.Parse("c99fbe35-d8e6-4e75-a579-23f9bcdfd69e");

    public override OnDemandMetadataProvider GetMetadataProvider(MetadataRequestOptions options)
    {
        throw new NotImplementedException();
    }

    public override string Name => "Wikipedia Categories";
    public override List<MetadataField> SupportedFields { get; } = [MetadataField.Tags];
}
