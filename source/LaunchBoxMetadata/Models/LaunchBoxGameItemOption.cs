using Playnite.SDK;
using System.Collections.Generic;

namespace LaunchBoxMetadata.Models;

public class LaunchBoxGameItemOption : GenericItemOption
{
    public LaunchBoxGameSearchResult Game { get; set; }

    public static LaunchBoxGameItemOption FromLaunchBoxGame(LaunchBoxGameSearchResult g)
    {
        var name = g.Name;
        if (g.MatchedName != g.Name)
            name += $" ({g.MatchedName})";

        var descriptionItems = new List<string>();
        if (g.ReleaseDate.HasValue)
            descriptionItems.Add(g.ReleaseDate.Value.ToString("yyyy-MM-dd"));
        if (g.ReleaseYear != 0)
            descriptionItems.Add(g.ReleaseYear.ToString());
        descriptionItems.Add(g.Platform);

        string description = string.Join(" | ", descriptionItems);

        return new LaunchBoxGameItemOption
        {
            Game = g,
            Name = name,
            Description = description,
        };
    }
}
