using System.Windows;
using System.Windows.Controls;

namespace ImportAnalyzer;

public partial class TagMissingGamesPromptView : UserControl
{
    private ImportAnalyzerPlugin ImportAnalyzerPlugin { get; }
    private Window Window { get; }
    private TagMissingGamesPromptViewModel ViewModel { get; }

    public TagMissingGamesPromptView(ImportAnalyzerPlugin importAnalyzerPlugin, TagMissingGamesPromptViewModel vm, Window window)
    {
        ImportAnalyzerPlugin = importAnalyzerPlugin;
        Window = window;
        DataContext = ViewModel = vm;
        InitializeComponent();
    }

    private void OkClick(object sender, RoutedEventArgs e)
    {
        ImportAnalyzerPlugin.TagMissingGames(ViewModel);
        Window.Close();
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        Window.Close();
    }
}
