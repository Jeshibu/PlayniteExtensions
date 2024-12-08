using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Barnite
{
    public partial class BarcodeResultsGrid : UserControl
    {
        public List<BarcodeResultEntry> Entries { get; }

        public BarcodeResultsGrid(Window window, BarcodeResultsGridViewModel viewModel)
        {
            Entries = viewModel.ResultEntries;
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
