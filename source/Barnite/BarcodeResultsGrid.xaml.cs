using System.Windows;
using System.Windows.Controls;

namespace Barnite;

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

    private void Retry_Click(object sender, RoutedEventArgs e)
    {
        Window.Close();
        var viewModel = (BarcodeResultsGridViewModel) DataContext;
        viewModel.RetryFailedCommand.Execute(viewModel.ResultEntries);
    }
}
