using System.Collections.Generic;
using System.Linq;
using Playnite.SDK;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;

namespace LaunchBoxMetadata.GenreImport;

public class GenreBulkImport(IPlayniteAPI playniteApi, GenreSearchProvider dataSource, IPlatformUtility platformUtility, LaunchBoxMetadataSettings settings)
: BulkGamePropertyAssigner<Genre, GamePropertyImportViewModel>(
    playniteApi.Database,
    new(playniteApi) { AllowEmptySearchQuery = true },
    dataSource,
    platformUtility,
    new WikipediaIdUtility(),
    ExternalDatabase.Wikipedia,
    settings.MaxDegreeOfParallelism
)
{
    public override string MetadataProviderName => "LaunchBox";

    protected override PropertyImportSetting GetPropertyImportSetting(Genre searchItem, out string name)
    {
        name = searchItem.Name;
        return new() { ImportTarget = PropertyImportTarget.Genres };
    }

    protected override string GetGameIdFromUrl(string url) => null;

    protected override IEnumerable<PotentialLink> GetPotentialLinks(Genre searchItem)
    {
        yield return new("Wikipedia", gd => gd.Links.FirstOrDefault(l => l.Name == "Wikipedia")?.Url) { Checked = settings.UseWikipediaLink };
        yield return new("Video", gd => gd.Links.FirstOrDefault(l => l.Name == "Video")?.Url) { Checked = settings.UseVideoLink };
    }
}
