using FilterSearch.Helpers;
using FilterSearch.Settings;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace FilterSearch;

public class FilterSearch : GenericPlugin
{
    public FilterSearch(IPlayniteAPI playniteApi) : base(playniteApi)
    {
        Properties = new GenericPluginProperties { HasSettings = true };
        Settings = new(this, PlayniteApi);
        Searches = new FilterSearchItemFactory(playniteApi, Settings.Settings).GetSearchSupports();
    }

    public override Guid Id => new("bd6bdf7f-86d8-4fa9-b056-29c8753d475f");
    private FilterSearchSettingsViewModel Settings { get; }

    public override void OnApplicationStarted(OnApplicationStartedEventArgs args) => WindowHelper.Init();

    public override IEnumerable<SearchItem> GetSearchGlobalCommands() => new FilterSearchItemFactory(PlayniteApi, Settings.Settings).GetSearchGlobalCommands();

    public override ISettings GetSettings(bool firstRunSettings) => Settings;
    public override UserControl GetSettingsView(bool firstRunView) => new FilterSearchSettingsView();
}
