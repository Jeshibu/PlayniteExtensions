using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using System.Linq;

namespace WikipediaCategories.BulkImport;

public class WikipediaCategoryBulkImport : BulkGamePropertyAssigner<WikipediaSearchResult, GamePropertyImportViewModel>
{
    public WikipediaCategoryBulkImport(IGameDatabaseAPI db, BulkPropertyUserInterface ui, WikipediaCategorySearchProvider dataSource, IPlatformUtility platformUtility, int maxDegreeOfParallelism = 8)
        : base(db, ui, dataSource, platformUtility, new WikipediaIdUtility(), ExternalDatabase.Wikipedia, maxDegreeOfParallelism)
    {
        Ui.AllowEmptySearchQuery = false;
        Ui.DefaultSearch = "Video games set in";
    }

    public override string MetadataProviderName => "Wikipedia";

    protected override PropertyImportSetting GetPropertyImportSetting(WikipediaSearchResult searchItem, out string name)
    {
        name = searchItem?.Name?.Split([':'], 2).Last();
        return new() { ImportTarget = PropertyImportTarget.Tags };
    }
}
