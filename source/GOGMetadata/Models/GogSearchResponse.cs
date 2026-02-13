using Playnite.SDK.Models;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace GOGMetadata.Models;

public class GogSearchResponse
{
    public class Product : IGameSearchResult
    {
        public string id;
        public string slug;
        public string title;
        public string releaseDate;
        public string coverHorizontal;
        public string coverVertical;
        public List<string> operatingSystems;
        public List<string> developers;
        public List<string> publishers;
        public List<string> screenshots;
        public List<SluggedName> features;
        public List<SluggedName> genres;
        public List<SluggedName> tags;
        public int reviewsRating;

        string IGameSearchResult.Name => title;

        string IGameSearchResult.Title => title;

        IEnumerable<string> IGameSearchResult.AlternateNames => [];

        IEnumerable<string> IGameSearchResult.Platforms => operatingSystems;

        public ReleaseDate? ReleaseDate
        {
            get
            {
                if (!DateTime.TryParseExact(releaseDate, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                    return null;

                if (date is { Month: 1, Day: 1 })
                    return new ReleaseDate(date.Year);

                return new ReleaseDate(date);
            }
        }
    }

    public List<Product> products;
}
