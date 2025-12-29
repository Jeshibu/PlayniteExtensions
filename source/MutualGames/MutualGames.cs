using MutualGames.Clients;
using MutualGames.Models.Settings;
using MutualGames.Views.Settings;
using Playnite.SDK;
using Playnite.SDK.Plugins;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace MutualGames;

public class MutualGames : GenericPlugin
{
    //So for anyone using GongSolutions.Wpf.DragDrop - be aware you have to instantiate something from it before referencing the package in your XAML
    private readonly GongSolutions.Wpf.DragDrop.DefaultDragHandler dropInfo = new();
    private static readonly ILogger logger = LogManager.GetLogger();

    private MutualGamesSettingsViewModel Settings => field ??= new(this);

    public override Guid Id { get; } = Guid.Parse("c615a8d1-c262-430a-b74b-6302d3328466");

    public string Name => "Mutual Games";

    public MutualGames(IPlayniteAPI api) : base(api)
    {
        Properties = new GenericPluginProperties
        {
            HasSettings = true
        };
    }

    public override IEnumerable<MainMenuItem> GetMainMenuItems(GetMainMenuItemsArgs args)
    {
        var section = $"@{Name}";
        yield return new() { Description = "Import mutual games from friend accounts", Action = ImportAccounts, MenuSection = section };
        yield return new() { Description = "Import mutual games from friend's exported file", Action = ImportFile, MenuSection = section };
        yield return new() { Description = "Export games file to send to friends", Action = ExportFile, MenuSection = section };
    }

    private void ImportAccounts(MainMenuItemActionArgs args)
    {
        var importer = new MutualGamesAccountImporter(PlayniteApi, Settings.Settings, GetClients());
        importer.Import();
    }

    private void ImportFile(MainMenuItemActionArgs args)
    {
        var importer = new MutualGamesFileImporter(PlayniteApi, Settings.Settings, new PlatformUtility(PlayniteApi));
        importer.Import();
    }

    private void ExportFile(MainMenuItemActionArgs args)
    {
        var exporter = new MutualGamesFileExporter(PlayniteApi, Settings.Settings);
        exporter.Export();
    }

    public override ISettings GetSettings(bool firstRunSettings) => Settings;

    public override UserControl GetSettingsView(bool firstRunSettings) => new MutualGamesSettingsView();

    public IEnumerable<IFriendsGamesClient> GetClients()
    {
        var webView = new OffScreenWebViewWrapper(PlayniteApi);
        yield return new GogClient(webView);
        yield return new SteamClient(PlayniteApi);
    }
}
