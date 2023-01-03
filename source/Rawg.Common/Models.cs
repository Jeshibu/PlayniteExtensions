using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Rawg.Common
{
    public class RawgObject
    {
        public int Id { get; set; }
        public string Slug { get; set; }
        public string Name { get; set; }
    }

    public class RawgLocalizedObject : RawgObject
    {
        /// <summary>
        /// https://en.wikipedia.org/wiki/List_of_ISO_639-2_codes
        /// </summary>
        public string Language { get; set; }
    }

    public class RawgPlatform
    {
        public RawgObject Platform { get; set; }
    }

    public class RawgGameBase : RawgObject
    {
        public string Released { get; set; }

        public RawgPlatform[] Platforms { get; set; }

        [JsonProperty("background_image")]
        public string BackgroundImage { get; set; }

        public float? Rating { get; set; }

        public int? Metacritic { get; set; }

        public RawgLocalizedObject[] Tags { get; set; }
        public RawgObject[] Genres { get; set; }

        //below properties are only in calls with a user token, not those with an api key
        [JsonProperty("user_game")]
        public RawgUserGame UserGame { get; set; }

        [JsonProperty("user_rating")]
        public int UserRating { get; set; }
    }

    public class RawgResult<T>
    {
        public T[] Results { get; set; }
        public int Count { get; set; }
        public string Next { get; set; }
        public string Previous { get; set; }
    }

    public class RawGameSearchResult
    {
        public RawgGameBase[] Results { get; set; }
        public int Count { get; set; }
        public string Next { get; set; }
        public string Previous { get; set; }
    }

    public class RawgGameDetails : RawgGameBase
    {
        [JsonProperty("name_original")]
        public string NameOriginal { get; set; }
        public string Description { get; set; }
        public RawgObject[] Developers { get; set; }
        public RawgObject[] Publishers { get; set; }
        public string Website { get; set; }

        [JsonProperty("reddit_url")]
        public string RedditUrl { get; set; }

        [JsonProperty("background_image_additional")]
        public string BackgroundImageAdditional { get; set; }
    }

    public class RawgUserGame
    {
        public string Status { get; set; }
        public DateTime Added { get; set; }
        public RawgObject[] Platforms { get; set; }
    }

    public class RawgScreenshot
    {
        public int Id { get; set; }
        public string Image { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        [JsonProperty("is_deleted")]
        public bool IsDeleted { get; set; }
    }

    public class RawgCollection : RawgObject
    {
        [JsonProperty("games_count")]
        public int GamesCount { get; set; }
    }

    public class RawgUser
    {
        public string Username { get; set; }

        public string Slug { get; set; }

        [JsonProperty("game_count")]
        public int GameCount { get; set; }

        [JsonProperty("api_key")]
        public string ApiKey { get; set; }
    }
}
