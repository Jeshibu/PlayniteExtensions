namespace PlayniteExtensions.Metadata.Common;

public class PropertyImportSetting
{
    public string Prefix { get; set; }
    public PropertyImportTarget ImportTarget { get; set; }
}

public enum PropertyImportTarget
{
    Ignore,
    Genres,
    Tags,
    Series,
    Features,
    Developers,
    Publishers,
}
