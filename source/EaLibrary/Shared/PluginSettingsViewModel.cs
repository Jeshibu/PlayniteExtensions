using Playnite.SDK.Data;
using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace Playnite.SDK;

public class PluginSettingsViewModel<TSettings, TPlugin> : ObservableObject, ISettings
    where TSettings : class
    where TPlugin : Plugin
{
    public readonly ILogger Logger = LogManager.GetLogger();
    public IPlayniteAPI PlayniteApi { get; set; }
    public TPlugin Plugin { get; set; }
    public TSettings EditingClone { get; set; }

    public TSettings Settings { get; set => SetValue(ref field, value); }

    public PluginSettingsViewModel(TPlugin plugin, IPlayniteAPI playniteApi)
    {
        Plugin = plugin;
        PlayniteApi = playniteApi;
    }

    public virtual void BeginEdit()
    {
        EditingClone = Serialization.GetClone(Settings);
    }

    public virtual void CancelEdit()
    {
        Settings = EditingClone;
    }

    public virtual void EndEdit()
    {
        Plugin.SavePluginSettings(Settings);
    }

    public TSettings LoadSavedSettings()
    {
        return Plugin.LoadPluginSettings<TSettings>();
    }

    public virtual bool VerifySettings(out List<string> errors)
    {
        errors = [];
        return true;
    }
}
