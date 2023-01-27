using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViveportLibrary.Api
{
    public class CmsAppDetailsResponse
    {
        public CmsProducts Products { get; set; }
        public CmsContents[] Contents { get; set; } = new CmsContents[0];
    }

    public class CmsProducts
    {
        public string[] Ids { get; set; } = new string[0];
        public int Total { get; set; }
        public int Size { get; set; }
        public int From { get; set; }
    }

    public class CmsContents
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Desc { get; set; }

        [JsonProperty("release_time")]
        public long ReleaseTimeMilliseconds { get; set; }

        public string[] Countries { get; set; }

        [JsonProperty("app_ids")]
        public string[] AppIds { get; set; }

        [JsonProperty("plan_plan_id")]
        public string PlanId { get; set; }

        [JsonProperty("plan_title")]
        public string PlanTitle { get; set; }

        [JsonProperty("plan_photo_url")]
        public string PlanPhotoUrl { get; set; }

        [JsonProperty("plan_type")]
        public string PlanType { get; set; }

        [JsonProperty("title_language")]
        public string TitleLanguage { get; set; }

        public ViveportApp[] Apps { get; set; } = new ViveportApp[0];

        public int Type { get; set; }

        [JsonProperty("p_publish_seconds")]
        public long PublishDateSeconds { get; set; }
    }

    public class ViveportApp
    {
        public string Id { get; set; }

        [JsonProperty("app_key")]
        public string AppKey { get; set; }

        [JsonProperty("ver_code")]
        public long VersionCode { get; set; }

        [JsonProperty("ver_name")]
        public string VersionName { get; set; }

        public string Title { get; set; }
        public string Desc { get; set; }

        public ViveportAuthor Author { get; set; }

        public string Categories { get; set; }

        public ViveportThumbnails Thumbnails { get; set; }

        public ViveportMedia[] Gallery { get; set; } = new ViveportMedia[0];

        public string[] Genres { get; set; } = new string[0];

        [JsonProperty("sys_reqs")]
        public SystemRequirements SystemRequirements { get; set; }

        [JsonProperty("release_time")]
        public long ReleaseTimeMilliseconds { get; set; }

        [JsonProperty("create_time")]
        public long CreateTimeMilliseconds { get; set; }

        [JsonProperty("content_rating")]
        public int ContentRating { get; set; }

        [JsonProperty("single_player")]
        public bool SinglePlayer { get; set; }

        [JsonProperty("release_note")]
        public string ReleaseNote { get; set; }

        [JsonProperty("is_free")]
        public bool IsFree { get; set; }

        public string Contact { get; set; }

        [JsonProperty("play_area")]
        public string[] PlayArea { get; set; } = new string[0];

        [JsonProperty("extra_docs")]
        public KeyValueWithAttributes[] ExtraDocs { get; set; } = new KeyValueWithAttributes[0];

        [JsonProperty("app_type")]
        public int AppType { get; set; }

        [JsonProperty("input_methods")]
        public string[] InputMethods { get; set; } = new string[0];

        public string Publisher { get; set; }

        [JsonProperty("player_num")]
        public string[] PlayerNum { get; set; } = new string[0];

        [JsonProperty("hw_matrix")]
        public HardwareMatrix HardwareMatrix { get; set; }

        [JsonProperty("developer_display_name")]
        public string DeveloperDisplayName { get; set; }

        public ViveportCloudData Cloud { get; set; }
    }

    public class ViveportCloudData
    {
        public ViveportCloudObject[] Objs { get; set; } = new ViveportCloudObject[0];
    }

    public class ViveportCloudObject
    {
        public string Type { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public string Url { get; set; }
    }

    public class ViveportAuthor
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Icon { get; set; }
        public string Desc { get; set; }
        public string Contact { get; set; }
    }

    public class ViveportThumbnails
    {
        public ViveportMedia Small { get; set; }
        public ViveportMedia Medium { get; set; }
        public ViveportMedia Large { get; set; }
        public ViveportMedia Square { get; set; }
    }

    public class ViveportMedia
    {
        [JsonProperty("media_type")]
        public int MediaType { get; set; }
        public string Url { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public string Cover { get; set; }
    }

    public class SystemRequirements
    {
        public string[] OS { get; set; } = new string[0];
        public string[] OSBits { get; set; } = new string[0];

        public OtherSystemRequirements Others { get; set; }
    }

    public class OtherSystemRequirements
    {
        public string Processor { get; set; }

        [JsonProperty("memory_size")]
        public string MemorySize { get; set; }

        [JsonProperty("memory_unit")]
        public string MemoryUnit { get; set; }

        [JsonProperty("directx_version")]
        public string DirectXVersion { get; set; }

        [JsonProperty("disk_space")]
        public string DiskSpace { get; set; }

        [JsonProperty("disk_space_unit")]
        public string DiskSpaceUnit { get; set; }

        public string Graphics { get; set; }
    }

    public class KeyValue
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class KeyValueWithAttributes : KeyValue
    {
        public KeyValue[] Attributes { get; set; } = new KeyValue[0];
    }

    public class HardwareMatrix
    {
        [JsonProperty("headset_features")]
        public string[] HeadsetFeatures { get; set; } = new string[0];
        public string[] Headsets { get; set; } = new string[0];
    }
}
