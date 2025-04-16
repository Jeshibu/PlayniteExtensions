using Newtonsoft.Json;
using System.Collections.Generic;

namespace MobyGamesMetadata.Api.V1
{
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

    public class GamePlatformDetails : GamePlatform
    {
        public List<MobyAttribute> Attributes { get; set; } = new List<MobyAttribute>();

        [JsonProperty("game_id")]
        public int GameId {  get; set; }

        public List<MobyGameRating> Ratings { get; set; } = new List<MobyGameRating>();

        public List<MobyGameRelease> Releases { get; set; } = new List<MobyGameRelease>();
    }

    public class MobyAttribute
    {
        [JsonProperty("attribute_category_id")]
        public int CategoryId { get; set; }

        [JsonProperty("attribute_category_name")]
        public string CategoryName { get; set; }

        [JsonProperty("attribute_id")]
        public int Id { get; set; }

        [JsonProperty("attribute_name")]
        public string Name { get; set; }
    }

    public class MobyGameRating
    {
        [JsonProperty("rating_id")]
        public int Id { get; set; }

        [JsonProperty("rating_name")]
        public string Name { get; set; }

        [JsonProperty("rating_system_id")]
        public int SystemId { get; set; }

        [JsonProperty("rating_system_name")]
        public string SystemName { get; set; }
    }

    public class MobyGameRelease
    {
        public List<MobyCompany> Companies { get; set; } = new List<MobyCompany>();

        public List<string> Countries { get; set;} = new List<string>();

        public string Description { get; set; }

        [JsonProperty("product_codes")]
        public List<ProductCode> ProductCodes { get; set; } = new List<ProductCode>();

        [JsonProperty("release_date")]
        public string ReleaseDate {  get; set; }
    }

    public class MobyCompany
    {
        [JsonProperty("company_id")]
        public int Id { get; set; }

        [JsonProperty("company_name")]
        public string Name { get; set; }

        public string Role {  get; set; }
    }

    public class ProductCode
    {
        [JsonProperty("product_code")]
        public string Code { get; set; }

        [JsonProperty("product_code_type")]
        public string Type { get; set; }

        [JsonProperty("product_code_type_id")]
        public int TypeId {  get; set; }
    }
}
