using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MobyGamesMetadata.Api
{
    public class GroupsRoot
    {
        public List<MobyGroup> Groups { get; set; } = new List<MobyGroup>();
    }

    public class MobyGroup
    {
        [JsonProperty("group_id")]
        public int Id { get; set; }

        [JsonProperty("group_description")]
        public string Description { get; set; }

        [JsonProperty("group_name")]
        public string Name { get; set; }
    }

    public class GamesRoot
    {
        public List<MobyGame> Games { get; set; } = new List<MobyGame>();
    }

    public class MobyGamesApiError
    {
        public int Code { get; set; }
        public string Error { get; set; }
        public string Message { get; set; }
    }

    public class MobyGame
    {
        [JsonProperty("alternate_titles")]
        public List<AlternateGameTitle> AlternateTitles { get; set; } = new List<AlternateGameTitle>();

        public string Description { get; set; }

        public List<Genre> Genres { get; set; } = new List<Genre>();

        [JsonProperty("moby_score")]
        public double? MobyScore { get; set; }

        [JsonProperty("moby_url")]
        public string MobyUrl { get; set; }

        [JsonProperty("num_votes")]
        public int NumVotes { get; set; }

        [JsonProperty("official_url")]
        public string OfficialUrl { get; set; }

        [JsonProperty("game_id")]
        public int Id { get; set; }

        public List<GamePlatform> Platforms { get; set; } = new List<GamePlatform>();

        [JsonProperty("sample_cover")]
        public CoverImage SampleCover { get; set; }

        [JsonProperty("sample_screenshots")]
        public List<Screenshot> SampleScreenshots { get; set; } = new List<Screenshot>();

        public string Title { get; set; }
    }

    public class AlternateGameTitle
    {
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class Genre
    {
        [JsonProperty("genre_name")]
        public string Name { get; set; }

        [JsonProperty("genre_id")]
        public int Id { get; set; }

        [JsonProperty("genre_category")]
        public string Category { get; set; }

        [JsonProperty("genre_category_id")]
        public int CategoryId { get; set; }
    }

    public class GamePlatform
    {
        [JsonProperty("platform_id")]
        public int Id { get; set; }

        [JsonProperty("platform_name")]
        public string Name { get; set; }

        [JsonProperty("first_release_date")]
        public string FirstReleaseDate { get; set; }
    }

    public abstract class MobyImage
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public string Image { get; set; }

        [JsonProperty("thumbnail_image")]
        public string ThumbnailImage { get; set; }
    }

    public class CoverImage : MobyImage
    {
        public List<string> Platforms { get; set; } = new List<string>();
    }

    public class Screenshot : MobyImage
    {
        public string Caption { get; set; }
    }
}
