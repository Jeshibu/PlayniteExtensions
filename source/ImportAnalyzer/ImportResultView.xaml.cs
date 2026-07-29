using System.Windows;
using System.Windows.Controls;

namespace ImportAnalyzer;

public partial class ImportResultView : UserControl
{
    private LibraryImportResult Data { get; }
    private Window Window { get; }
    private readonly ImportAnalyzerPlugin importAnalyzerPlugin;

    public ImportResultView(ImportAnalyzerPlugin importAnalyzerPlugin, LibraryImportResult data, Window window)
    {
        Data = data;
        Window = window;
        DataContext = data;
        this.importAnalyzerPlugin = importAnalyzerPlugin;
        InitializeComponent();
    }

    private void TagMissingGames(object sender, RoutedEventArgs e) => importAnalyzerPlugin.TagMissingGames(Data);

    private void CloseWindow(object sender, RoutedEventArgs e) => Window.Close();

    private void Export(object sender, RoutedEventArgs e) => importAnalyzerPlugin.ExportImportResult(Data);
}

