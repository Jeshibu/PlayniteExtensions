using Playnite.SDK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace Barnite;

public class BarniteSettings : ObservableObject
{
    public ObservableCollection<ScraperSettings> Scrapers { get; set => SetValue(ref field, value); } = [];
}

public class ScraperSettings : ObservableObject
{
    public bool Enabled{ get; set => SetValue(ref field, value); }
    public string Name{ get; set => SetValue(ref field, value); }
    public string WebsiteUrl{ get; set => SetValue(ref field, value); }
    public int Order{ get; set => SetValue(ref field, value); }
}

public class BarniteSettingsViewModel : PluginSettingsViewModel<BarniteSettings, Barnite>
{
    public BarniteSettingsViewModel(Barnite plugin, IPlayniteAPI playniteAPI) : base(plugin, playniteAPI)
    {
        Settings = LoadSavedSettings() ?? new BarniteSettings();
    }

    public ICollectionView ScrapersView
    {
        get
        {
            if (field == null)
            {
                field = CollectionViewSource.GetDefaultView(Settings.Scrapers);
                field.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
            }

            return field;
        }
    }

    public RelayCommand<ScraperSettings> VisitWebsiteCommand
    {
        get
        {
            return field ??= new RelayCommand<ScraperSettings>((ss) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(ss.WebsiteUrl);
                }
                catch { }
            });
        }
    }

    public RelayCommand<ScraperSettings> MoveUpCommand
    {
        get
        {
            return field ??= new RelayCommand<ScraperSettings>((ss) =>
            {
                var items = ScrapersView.Cast<ScraperSettings>().ToList();
                var index = items.IndexOf(ss);
                if (index < 1)
                    return;
                int order1 = items[index - 1].Order;
                int order2 = items[index].Order;
                items[index - 1].Order = order2;
                items[index].Order = order1;
                OnPropertyChanged(nameof(Settings));
                ScrapersView.Refresh();
                OnPropertyChanged(nameof(ScrapersView));
            });
        }
    }

    public RelayCommand<ScraperSettings> MoveDownCommand
    {
        get
        {
            return field ??= new RelayCommand<ScraperSettings>((ss) =>
            {
                var items = ScrapersView.Cast<ScraperSettings>().ToList();
                var index = items.IndexOf(ss);
                if (index == -1 || index == items.Count - 1)
                    return;
                int order1 = items[index].Order;
                int order2 = items[index + 1].Order;
                items[index].Order = order2;
                items[index + 1].Order = order1;
                OnPropertyChanged(nameof(Settings));
                ScrapersView.Refresh();
                OnPropertyChanged(nameof(ScrapersView));
            });
        }
    }
}
