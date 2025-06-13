using Newtonsoft.Json;
using PlayniteExtensions.Metadata.Common;

namespace SteamTagsImporter.BulkImport;

public class SteamProperty : IHasName
{
    public string Name { get; set; }
    public string Category { get; set; }
    public string Param { get; set; }
    public string Value { get; set; }

    public override string ToString()
    {
        return JsonConvert.SerializeObject(this);
    }
}

public class SteamSearchResponse
{
    public int Success { get; set; }

    [JsonProperty("results_html")]
    public string ResultsHtml { get; set; }

    [JsonProperty("total_count")]
    public int TotalCount { get; set; }

    public int Start { get; set; }
}
