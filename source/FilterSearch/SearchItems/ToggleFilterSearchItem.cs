using FilterSearch.Helpers;
using FilterSearch.SearchItems.Base;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;

namespace FilterSearch.SearchItems;

public class ToggleFilterSearchItem : BaseFilterSearchItem
{
    private Action<FilterPreset> ToggleAction { get; }

    public ToggleFilterSearchItem(string name, IMainViewAPI mainViewApi, Action<FilterPreset> toggleAction) : base($"{name} (toggle)", mainViewApi)
    {
        ToggleAction = toggleAction;
        Description = "Filter setting";
        PrimaryAction = new("Toggle", Toggle);
    }

    private void Toggle()
    {
        var fp = MainView.GetFilterPreset();
        ToggleAction(fp);

        MainView.ApplyFilterPreset(fp);

        ShowLibraryView();
    }
}