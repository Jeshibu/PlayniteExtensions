using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Linq;

namespace WikipediaCategoryImport.BulkImport;

public class WikipediaCategoryBulkImport : BulkGamePropertyAssigner<WikipediaSearchResult,GamePropertyImportViewModel>
{
    public WikipediaCategoryBulkImport(IPlayniteAPI playniteApi, WikipediaCategorySearchProvider dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism = 8)
        : base(playniteApi, dataSource, platformUtility, new WikipediaIdUtility(), ExternalDatabase.Wikipedia, maxDegreeOfParallelism)
    {
        AllowEmptySearchQuery = false;
        DefaultSearch = "Video games set in ";
    }

    public override string MetadataProviderName => "Wikipedia";

    protected override PropertyImportSetting GetPropertyImportSetting(WikipediaSearchResult searchItem, out string name)
    {
        name = searchItem?.Name?.Split([':'], 2).Last();
        return new() { ImportTarget = PropertyImportTarget.Tags };
    }
}
