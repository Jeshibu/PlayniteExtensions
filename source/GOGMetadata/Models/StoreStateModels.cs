using System.Collections.Generic;

namespace GOGMetadata.Models;

public class CatalogData
{
    public List<SluggedName> Languages { get; set; }
    public List<SluggedName> Systems { get; set; }
    public List<SluggedName> Features { get; set; }
    public List<SluggedName> ReleaseStatuses { get; set; }
    public List<string> Types { get; set; }
    public List<SluggedName> FullGenresList { get; set; }
    public List<SluggedName> FullTagsList { get; set; }
}
