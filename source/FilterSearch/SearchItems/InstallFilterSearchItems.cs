using FilterSearch.Helpers;
using FilterSearch.SearchItems.Base;
using Playnite.SDK;

namespace FilterSearch.SearchItems;

public class InstallFilterSearchItem:BaseFilterSearchItem
{
    private bool Installed { get; }
    private bool Uninstalled { get; }

    public InstallFilterSearchItem(string name, IMainViewAPI mainViewApi, bool installed, bool uninstalled) : base(name, "Filter setting", mainViewApi)
    {
        Installed = installed;
        Uninstalled = uninstalled;
        PrimaryAction = new("Apply", Filter);
    }

    private void Filter()
    {
        var fp = MainView.GetFilterPreset();
        fp.Settings.IsInstalled = Installed;
        fp.Settings.IsUnInstalled = Uninstalled;
        MainView.ApplyFilterPreset(fp);
        
        ShowLibraryView();
    }
}
