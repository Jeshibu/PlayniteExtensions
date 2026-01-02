using Playnite.SDK.Models;
using System.Collections.Generic;

namespace PlayniteExtensions.Metadata.Common;

public interface IGameSearchResult
{
    /// <summary>
    /// Display name (inherited from Playnite's GenericItemOption)
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Actual name/title to use for game matching
    /// </summary>
    string Title { get; }

    IEnumerable<string> AlternateNames { get; }

    IEnumerable<string> Platforms { get; }

    ReleaseDate? ReleaseDate { get; }
}
