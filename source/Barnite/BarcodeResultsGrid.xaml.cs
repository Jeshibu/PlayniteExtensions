using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Barnite
{
    public partial class BarcodeResultsGrid : UserControl
    {
        public BarcodeResultsGrid(Window window, BarcodeResultsGridViewModel viewModel)
        {
            DataContext = viewModel;
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
