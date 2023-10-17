using Newtonsoft.Json;

namespace ViveportLibrary.Api
{
    public class GetCustomAttributeResponseRoot
    {
        public CustomAttributeMetadataContainer Data { get; set; }
    }

    public class CustomAttributeMetadataContainer
    {
        public CustomAttributeMetadata CustomAttributeMetadata { get; set; }
    }

    public class CustomAttributeMetadata
    {
        public CustomAttributeMetadataItem[] Items { get; set; } = new CustomAttributeMetadataItem[0];
    }

    public class CustomAttributeMetadataItem
    {
        [JsonProperty("attribute_code")]
        public string AttributeCode { get; set; }

        [JsonProperty("attribute_options")]
        public AttributeOption[] AttributeOptions { get; set; } = new AttributeOption[0];
    }

    public class AttributeOption
    {
        [JsonProperty("admin_label")]
        public string AdminLabel { get; set; }
        public string Value { get; set; }
        public string Label { get; set; }
    }
}
