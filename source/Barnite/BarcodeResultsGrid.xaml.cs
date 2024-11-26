using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Barnite
{
    public partial class BarcodeResultsGrid : UserControl
    {
        public List<BarcodeEntry> Entries { get; }

        public BarcodeResultsGrid(Window window, List<BarcodeEntry> entries)
        {
            Entries = entries;
            DataContext = this;  // Set DataContext for binding
            Window = window;
            InitializeComponent();
        }

        public Window Window { get; }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            Window.DialogResult = true;
            Window.Close();
        }
    }
}
