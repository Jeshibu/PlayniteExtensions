using Playnite.SDK.Data;
using Playnite.SDK.Plugins;
using System.Collections.Generic;

namespace Playnite.SDK;

//Shamelessly stolen from https://github.com/JosefNemec/PlayniteExtensions/blob/master/source/Generic/PlayniteExtensions.Common/PluginSettingsViewModel.cs
public abstract class PluginSettingsViewModel<TSettings, TPlugin>(TPlugin plugin, IPlayniteAPI playniteApi) : ObservableObject, ISettings
    where TSettings : class
    where TPlugin : Plugin
{
    protected readonly ILogger Logger = LogManager.GetLogger();
    public IPlayniteAPI PlayniteApi { get; set; } = playniteApi;
    protected TPlugin Plugin { get; set; } = plugin;
    protected TSettings EditingClone { get; set; }
    public TSettings Settings { get; protected set => SetValue(ref field, value); }

    public virtual void BeginEdit() => EditingClone = Serialization.GetClone(Settings);

    public void CancelEdit() => Settings = EditingClone;

    public virtual void EndEdit() => Plugin.SavePluginSettings(Settings);

    protected TSettings LoadSavedSettings() => Plugin.LoadPluginSettings<TSettings>();

    public virtual bool VerifySettings(out List<string> errors)
    {
        errors = [];
        return true;
    }
}
