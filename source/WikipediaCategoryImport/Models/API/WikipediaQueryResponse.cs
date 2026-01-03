using System.Collections.Generic;

namespace WikipediaCategoryImport.Models.API;

public class WikipediaQueryResponse<TQuery>
{
    public object batchcomplete { get; set; }
    public Dictionary<string, string> @continue { get; set; }
    public TQuery query { get; set; }
}
