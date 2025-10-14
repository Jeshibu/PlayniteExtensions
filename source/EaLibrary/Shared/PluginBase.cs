using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace Playnite.SDK;

public abstract class LibraryPluginBase<TSettings> : LibraryPlugin
    where TSettings : ISettings
{
    private Func<bool, UserControl> GetSettingsViewAction { get; }

    public readonly ILogger Logger;

    public string ImportErrorMessageId { get; }
    public override string Name { get; }
    public override Guid Id { get; }
    public override LibraryClient Client { get; }
    public override string LibraryIcon { get; }

    public TSettings SettingsViewModel { get; set; }

    public LibraryPluginBase(
        string name,
        Guid id,
        LibraryPluginProperties properties,
        LibraryClient client,
        string libraryIcon,
        Func<bool, UserControl> getSettingsViewAction,
        IPlayniteAPI api) : base(api)
    {
        Logger = LogManager.GetLogger(GetType().Name);
        Name = name;
        Id = id;
        ImportErrorMessageId = $"{name}_libImportError";
        Properties = properties;
        Client = client;
        LibraryIcon = libraryIcon;
        GetSettingsViewAction = getSettingsViewAction;
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        if (SettingsViewModel != null)
        {
            return SettingsViewModel;
        }

        return base.GetSettings(firstRunSettings);
    }

    public override UserControl GetSettingsView(bool firstRunView)
    {
        if (GetSettingsViewAction != null)
        {
            return GetSettingsViewAction(firstRunView);
        }

        return base.GetSettingsView(firstRunView);
    }
}
