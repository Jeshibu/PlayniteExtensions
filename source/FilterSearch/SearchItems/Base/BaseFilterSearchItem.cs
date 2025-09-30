using FilterSearch.Helpers;
using Playnite.SDK;
using Playnite.SDK.Plugins;

namespace FilterSearch.SearchItems.Base;

public abstract class BaseFilterSearchItem : SearchItem
{
    protected IMainViewAPI MainView { get; }

    protected BaseFilterSearchItem(string name, IMainViewAPI mainViewApi) : base(name, null)
    {
        MainView = mainViewApi;
    }

    protected BaseFilterSearchItem(string name, string description, IMainViewAPI mainViewApi) : this(name, mainViewApi)
    {
        Description = description;
    }

    protected void ShowLibraryView()
    {
        WindowHelper.BringMainWindowToForeground();
        MainView.SwitchToLibraryView();
    }
}