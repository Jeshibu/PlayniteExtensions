using Playnite.SDK.Models;
using System.Collections.Generic;

namespace PlayniteExtensions.Metadata.Common
{
    public interface IGameSearchResult
    {
        string Name { get; }
        IEnumerable<string> AlternateNames { get; }
        IEnumerable<string> Platforms { get; }
        ReleaseDate? ReleaseDate { get; }
    }
}