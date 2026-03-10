using System.Collections.Generic;

namespace IgnMetadata.HowLongToBeat.Models;

public class IgnHltbDataModel
{
    public string Name { get; set; }
    public string CoverUrl { get; set; }
    public string IgnUrl { get; set; }
    public string HltbUrl { get; set; }
    public int MainStoryHours { get; set; }
    public int MainStoryAndSidesHours { get; set; }
    public int EverythingHours { get; set; }
    public int AllStylesHours { get; set; }
    public List<string> Platforms { get; set; } = [];
}
