using System.Windows;
using System.Windows.Controls;

namespace SteamTagsImporter
{
    /// <summary>
    /// Interaction logic for GamePropertyImportView.xaml
    /// </summary>
    public partial class GamePropertyImportView : UserControl
    {
        public GamePropertyImportView(Window window)
        {
            InitializeComponent();
            Window = window;
        }

        public Window Window { get; }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Window.DialogResult = true;
            Window.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Window.DialogResult = false;
            Window.Close();
        }
    }
}
