using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ExtraEmulatorProfiles;

public class ExtraEmulatorProfiles : GenericPlugin
{
    private static readonly ILogger logger = LogManager.GetLogger();

    private ExtraEmulatorProfilesSettingsViewModel settings { get; set; }

    public override Guid Id { get; } = Guid.Parse("e4bf38d6-d2fd-4741-a269-47becfd55433");

    public ExtraEmulatorProfiles(IPlayniteAPI api) : base(api)
    {
        settings = new ExtraEmulatorProfilesSettingsViewModel(this);
        Properties = new GenericPluginProperties
        {
            HasSettings = true
        };
    }

    public override void OnApplicationStarted(OnApplicationStartedEventArgs args)
    {
        if (settings.Settings.InstalledPatchVersion < settings.PluginVersion)
            settings.ExecutePatch();
    }

    public override ISettings GetSettings(bool firstRunSettings)
    {
        return settings;
    }

    public override UserControl GetSettingsView(bool firstRunSettings)
    {
        return new ExtraEmulatorProfilesSettingsView();
    }

    public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
    {
        yield return new MainMenuItem { MenuSection = "@Extra Emulator Profiles", Description = "Add and update emulator profiles", Action = _ => settings.ExecutePatch() };
        yield return new MainMenuItem { MenuSection = "@Extra Emulator Profiles", Description = "Reset emulator profiles to defaults", Action = _ => settings.Reset() };
    }
}