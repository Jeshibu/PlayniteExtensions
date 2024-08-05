using MutualGames.Models.Export;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MutualGames.Views.Export
{
    /// <summary>
    /// Interaction logic for ExportFilePromptView.xaml
    /// </summary>
    public partial class ExportFilePromptView : UserControl
    {
        private Window Window { get; }

        public ExportFilePromptView(Window window)
        {
            InitializeComponent();
            Window = window;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Window.DialogResult = true;
            Window.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Window.DialogResult = false;
            Window.Close();
        }

        public Dictionary<ExportGamesMode, string> Modes { get; } = new Dictionary<ExportGamesMode, string>()
        {
            { ExportGamesMode.Filtered, "Only currently visible (filtered)" },
            { ExportGamesMode.AllExcludeHidden, "All (exclude hidden)" },
            { ExportGamesMode.AllIncludeHidden, "All (include hidden)" },
        };

        public ExportGamesMode Mode { get; set; }
    }
}
