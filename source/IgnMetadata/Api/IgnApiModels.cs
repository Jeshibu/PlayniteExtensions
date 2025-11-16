using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;

namespace IgnMetadata.Api;

public class IgnResponseRoot<T>
{
    public T Data;
    public IgnError[] Errors = [];
}

public class IgnError
{
    public string Message;
}

public class IgnSearchResultData
{
    public IgnSearchResultObjects SearchObjectsByName;
}

public class IgnGetGameResultData
{
    public IgnGame ObjectSelectByTypeAndSlug;
}

public class IgnGetImagesResultData
{
    public IgnImageGalleryObject ImageGallery;
}

public class IgnSearchResultObjects
{
    public IgnGame[] Objects = [];
    public IgnPageInfo PageInfo;
}

public class IgnPageInfo
{
    public bool HasNext;
    public int? NextCursor;
    public int Total;
}

public class IgnGame : IGameSearchResult
{
    public string Id;
    public string Slug;
    public string Url;
    public IgnGameMetadata Metadata;
    public IgnUrlHolder PrimaryImage;
    public IgnAttribute[] Features = [];
    public IgnAttribute[] Franchises = [];
    public IgnAttribute[] Genres = [];
    public IgnAttribute[] Producers = [];
    public IgnAttribute[] Publishers = [];
    public IgnObjectRegion[] ObjectRegions = [];

    public List<string> Names
    {
        get
        {
            var namesObj = Metadata.Names;
            var names = new List<string> { namesObj.Name.Trim() };

            if (!string.IsNullOrEmpty(namesObj.Short) && namesObj.Name != namesObj.Short)
                names.Add(namesObj.Short.Trim());

            if (namesObj.Alt?.Length > 0)
                names.AddRange(namesObj.Alt.Select(s => s.Trim()));

            return names;
        }
    }

    public string Name
    {
        get
        {
            var names = Names;
            var name = names.First();
            if (names.Count > 1)
                name += $" (AKA {string.Join(" / ", AlternateNames)})";
            return name;
        }
    }

    public string Title => Names.First();

    public IEnumerable<string> AlternateNames => Names.Skip(1);

    public IEnumerable<string> Platforms => ObjectRegions.SelectMany(r => r.Releases).SelectMany(r => r.PlatformAttributes).Select(x => x.Name).ToHashSet();

    public string ReleaseDateString
    {
        get
        {
            var releaseDates = ObjectRegions.SelectMany(r => r.Releases).Where(r => !string.IsNullOrWhiteSpace(r.Date)).Select(r => r.Date).OrderBy(d => d).ToList();
            return releaseDates.FirstOrDefault();
        }
    }

    public ReleaseDate? ReleaseDate
    {
        get
        {
            var dateString = ReleaseDateString;
            if (!string.IsNullOrWhiteSpace(dateString) && DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out DateTime date))
                return new ReleaseDate(date);
            else
                return null;
        }
    }

    public IEnumerable<string> AgeRatings
    {
        get
        {
            foreach (var region in ObjectRegions)
            {
                if (region.AgeRating == null)
                    continue;

                yield return $"{region.AgeRating.AgeRatingType} {region.AgeRating.Name}";
            }
        }
    }
}

public class IgnObjectRegion
{
    /// <summary>
    /// The game's name for this particular release - often empty
    /// </summary>
    public string Name;
    public string Region;
    public IgnRelease[] Releases = [];

    /// <summary>
    /// Not in search results
    /// </summary>
    public IgnAgeRating AgeRating;

    /// <summary>
    /// Not in search results
    /// </summary>
    public IgnAttribute[] AgeRatingDescriptors = [];
}

public class IgnAgeRating
{
    public string Name;
    public string AgeRatingType;
}

public class IgnRelease
{
    public string Date;
    public bool EstimatedDate;
    public IgnAttribute[] PlatformAttributes = [];
}

public class IgnGameMetadata
{
    public IgnNameData Names;

    /// <summary>
    /// Not in search results
    /// </summary>
    public IgnDescriptions Descriptions;

    /// <summary>
    /// Not in search results
    /// </summary>
    public string State;
}

public class IgnDescriptions
{
    public string Long;
    public string Short;
}

public class IgnNameData
{
    public string Name;
    public string Short;
    public string[] Alt = [];
}

public class IgnUrlHolder
{
    public string Url;
}

public class IgnAttribute
{
    public string Name;
    public string Slug;
}

public class IgnImageGalleryObject
{
    public IgnPageInfo PageInfo;
    public IgnImage[] Images = [];
}

public class IgnImage
{
    public string Id;
    public string Caption;
    public string Url;
}