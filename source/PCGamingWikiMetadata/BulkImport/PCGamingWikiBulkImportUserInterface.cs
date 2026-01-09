using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PCGamingWikiMetadata.BulkImport;

public class PCGamingWikiBulkImportUserInterface : BulkPropertyUserInterface
{
    private readonly IPlayniteAPI _playniteApi;

    public PCGamingWikiBulkImportUserInterface(IPlayniteAPI playniteApi) : base(playniteApi)
    {
        _playniteApi = playniteApi;
        AllowEmptySearchQuery = true;
    }

    public virtual SelectStringsViewModel SelectString(SelectStringsViewModel vm)
    {
        var window = _playniteApi.Dialogs.CreateWindow(new WindowCreationOptions { ShowCloseButton = true, ShowMaximizeButton = true, ShowMinimizeButton = false });
        var view = new SelectStringsView(window) { DataContext = vm };
        window.Content = view;
        window.SizeToContent = SizeToContent.WidthAndHeight;
        window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        window.Title = "Select games";
        windowSizedDown = false;
        window.SizeChanged += Window_SizeChanged;
        bool? dialogResult = window.ShowDialog();
        return dialogResult == true ? vm : null;
    }

    public TItem ChooseItemWithSearch<TItem>(List<GenericItemOption<TItem>> items, Func<string, List<GenericItemOption>> searchFunction, string defaultSearch = null, string caption = null) where TItem : class
    {
        var chosenItem = _playniteApi.Dialogs.ChooseItemWithSearch(items?.Cast<GenericItemOption>().ToList(), searchFunction, defaultSearch, caption) as GenericItemOption<TItem>;
        return chosenItem?.Item;
    }
}
