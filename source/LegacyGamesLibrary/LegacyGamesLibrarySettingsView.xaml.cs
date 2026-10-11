using Playnite.SDK;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace LegacyGamesLibrary;

public partial class LegacyGamesLibrarySettingsView : UserControl
{
    private readonly ILogger logger = LogManager.GetLogger();

    public LegacyGamesLibrarySettingsView()
    {
        InitializeComponent();
    }

    private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"Error navigating to {e?.Uri?.AbsoluteUri}");
        }
    }
}
