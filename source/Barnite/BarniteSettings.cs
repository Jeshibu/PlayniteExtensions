using Playnite.SDK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;

namespace Barnite;

public class BarniteSettings : ObservableObject
{
    private ObservableCollection<ScraperSettings> scrapers;

    public ObservableCollection<ScraperSettings> Scrapers { get => scrapers; set => SetValue(ref scrapers, value); }

    public BarniteSettings()
    {
        scrapers = new ObservableCollection<ScraperSettings>();
    }
}

public class ScraperSettings : ObservableObject
{
    private bool enabled;
    private string name;
    private string websiteUrl;
    private int order;

    public bool Enabled { get => enabled; set => SetValue(ref enabled, value); }
    public string Name { get => name; set => SetValue(ref name, value); }
    public string WebsiteUrl { get => websiteUrl; set => SetValue(ref websiteUrl, value); }
    public int Order { get => order; set => SetValue(ref order, value); }
}

public class BarniteSettingsViewModel : PluginSettingsViewModel<BarniteSettings, Barnite>
{
    public BarniteSettingsViewModel(Barnite plugin, IPlayniteAPI playniteAPI) : base(plugin, playniteAPI)
    {
        // Load saved settings.
        var savedSettings = plugin.LoadPluginSettings<BarniteSettings>();

        // LoadPluginSettings returns null if not saved data is available.
        if (savedSettings != null)
        {
            Settings = savedSettings;
        }
        else
        {
            Settings = new BarniteSettings();
        }
    }

    private ICollectionView scrapersView;
    public ICollectionView ScrapersView
    {
        get
        {
            if (scrapersView == null)
            {
                scrapersView = CollectionViewSource.GetDefaultView(Settings.Scrapers);
                scrapersView.SortDescriptions.Add(new SortDescription("Order", ListSortDirection.Ascending));
            }

            return scrapersView;
        }
    }

    private RelayCommand<ScraperSettings> visitWebsiteCommand;
    public RelayCommand<ScraperSettings> VisitWebsiteCommand
    {
        get
        {
            return visitWebsiteCommand ?? (visitWebsiteCommand = new RelayCommand<ScraperSettings>((ss) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(ss.WebsiteUrl);
                }
                catch { }
            }));
        }
    }

    private RelayCommand<ScraperSettings> moveUpCommand;
    public RelayCommand<ScraperSettings> MoveUpCommand
    {
        get
        {
            return moveUpCommand ?? (moveUpCommand = new RelayCommand<ScraperSettings>((ss) =>
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
            }));
        }
    }

    private RelayCommand<ScraperSettings> moveDownCommand;
    public RelayCommand<ScraperSettings> MoveDownCommand
    {
        get
        {
            return moveDownCommand ?? (moveDownCommand = new RelayCommand<ScraperSettings>((ss) =>
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
            }));
        }
    }
}