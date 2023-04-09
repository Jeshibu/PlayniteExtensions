using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Security.Policy;
using System.Text;

namespace PlayniteExtensions.Common
{
    public class GameDetails
    {
        public List<string> Names { get; set; } = new List<string>();

        public string Description { get; set; }

        public ReleaseDate? ReleaseDate { get; set; }

        public List<Link> Links { get; set; } = new List<Link>();

        public int? CriticScore { get; set; }

        public int? CommunityScore { get; set; }

        public List<IImageData> IconOptions { get; set; } = new List<IImageData>();

        public List<IImageData> CoverOptions { get; set; } = new List<IImageData>();

        public List<IImageData> BackgroundOptions { get; set; } = new List<IImageData>();

        public List<string> Series { get; set; } = new List<string>();

        public List<string> AgeRatings { get; set; } = new List<string>();

        public List<MetadataProperty> Platforms { get; set; } = new List<MetadataProperty>();

        public List<string> Developers { get; set; } = new List<string>();

        public List<string> Publishers { get; set; } = new List<string>();

        public List<string> Genres { get; set; } = new List<string>();

        public List<string> Tags { get; set; } = new List<string>();

        public List<string> Features { get; set; } = new List<string>();

        public ulong? InstallSize { get; set; }

        public string Url { get; set; }
    }

    public class GenericItemOption<T> : GenericItemOption
    {
        public GenericItemOption(T item)
        {
            Item = item;
        }

        public T Item { get; }
    }

    public interface ISearchableDataSource<TSearchResult>
    {
        IEnumerable<TSearchResult> Search(string query);
        GenericItemOption<TSearchResult> ToGenericItemOption(TSearchResult item);
    }

    public interface ISearchableDataSourceWithDetails<TSearchResult, TDetails> : ISearchableDataSource<TSearchResult>
    {
        TDetails GetDetails(TSearchResult searchResult);
    }

    public interface IImageData
    {
        string Url { get; }
        string ThumbnailUrl { get; }
        int Width { get; }
        int Height { get; }
        IEnumerable<string> Platforms { get; }
    }

    public class BasicImage : IImageData
    {
        public BasicImage(string url)
        {
            Url = url;
        }

        public string Url { get; set; }
        public string ThumbnailUrl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public IEnumerable<string> Platforms { get; set; } = new List<string>();
    }
}