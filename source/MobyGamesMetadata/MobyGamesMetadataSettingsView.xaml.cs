using PlayniteExtensions.Metadata.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MobyGamesMetadata
{
    public partial class MobyGamesMetadataSettingsView : UserControl
    {
        public MobyGamesMetadataSettingsView()
        {
            InitializeComponent();
        }

        private void SetImportTarget_Click(object sender, RoutedEventArgs e)
        {
            if (!(DataContext is MobyGamesMetadataSettingsViewModel viewModel)
                || !(e.Source is MenuItem menuItem)
                || !Enum.TryParse<PropertyImportTarget>(menuItem.Header.ToString(), out var target))
                return;

            var selectedItems = GenreSettingsView.SelectedItems.Cast<MobyGamesGenreSetting>().ToList();

            viewModel.SetImportTarget(target, selectedItems);
        }
    }
}