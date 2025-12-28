using Playnite.SDK.Plugins;
using Playnite.SDK;

namespace PCGamingWikiMetadata.Tests;

public class TestMetadataRequestOptions : MetadataRequestOptions
{
    public TestMetadataRequestOptions(BuiltinExtension library) : base(null, false)
    {
        this.GameData = new()
        {
            PluginId = BuiltinExtensions.GetIdFromExtension(library)
        };
    }

    public static TestMetadataRequestOptions Steam() => new(BuiltinExtension.SteamLibrary);
    public static TestMetadataRequestOptions Origin() => new(BuiltinExtension.OriginLibrary);
    public static TestMetadataRequestOptions Xbox() => new(BuiltinExtension.XboxLibrary);
    public static TestMetadataRequestOptions Epic() => new(BuiltinExtension.EpicLibrary);
    public static TestMetadataRequestOptions BattleNet() => new(BuiltinExtension.BattleNetLibrary);
}
