using System.Windows;
using System.Windows.Controls;

namespace PCGamingWikiBulkImport.Views
{
    /// <summary>
    /// Interaction logic for SelectStringsView.xaml
    /// </summary>
    public partial class SelectStringsView : UserControl
    {
        public SelectStringsView(Window window)
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
