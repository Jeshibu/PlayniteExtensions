using System.Windows;
using System.Windows.Controls;

namespace ImportAnalyzer;

public partial class ImportResultView : UserControl
{
    private readonly ImportAnalyzerPlugin importAnalyzerPlugin;

    public ImportResultView(ImportAnalyzerPlugin importAnalyzerPlugin)
    {
        this.importAnalyzerPlugin = importAnalyzerPlugin;
        InitializeComponent();
    }

    private void TagMissingGames(object sender, RoutedEventArgs e)
    {
        importAnalyzerPlugin.TagMissingGames((LibraryImportResult)DataContext);
    }
}

