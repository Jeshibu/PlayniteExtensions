using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using Playnite.SDK;

public class TestMetadataRequestOptions : MetadataRequestOptions
{
    public TestMetadataRequestOptions() : base(null, false)
    {
        this.GameData = new Game();
        SetGameSourceSteam();
    }

    public void SetGameSourceSteam()
    {
        this.GameData.PluginId = BuiltinExtensions.GetIdFromExtension(BuiltinExtension.SteamLibrary);
    }

    public void SetGameSourceEpic()
    {
        this.GameData.PluginId = BuiltinExtensions.GetIdFromExtension(BuiltinExtension.EpicLibrary);
    }

    public void SetGameSourceXbox()
    {
        this.GameData.PluginId = BuiltinExtensions.GetIdFromExtension(BuiltinExtension.XboxLibrary);
    }

    public void SetGameSourceBattleNet()
    {
        this.GameData.PluginId = BuiltinExtensions.GetIdFromExtension(BuiltinExtension.BattleNetLibrary);
    }

    public void SetGameSourceOrigin()
    {
        this.GameData.PluginId = BuiltinExtensions.GetIdFromExtension(BuiltinExtension.OriginLibrary);
    }
}
