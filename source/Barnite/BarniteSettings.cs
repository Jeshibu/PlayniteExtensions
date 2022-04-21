using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;

namespace Barnite
{
    public class BarniteSettings : ObservableObject
    {
        private ObservableCollection<ScraperSettings> scrapers;

        public ObservableCollection<ScraperSettings> Scrapers { get => scrapers; set => SetValue(ref scrapers, value); }

        public BarniteSettings()
        {
            scrapers = new ObservableCollection<ScraperSettings>();
        }
    }

    public class ScraperSettings
    {
        public bool Enabled;
        public string Name;
        public string WebsiteUrl;
        public Type Type;
        public int Order;
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
                }));
            }
        }
    }
}