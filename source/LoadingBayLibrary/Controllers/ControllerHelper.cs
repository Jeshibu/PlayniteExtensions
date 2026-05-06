using LoadingBayLibrary.Services;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace LoadingBayLibrary.Controllers;

internal static class ControllerHelper
{
    public static string GetLoadingBayGamePageUrl(Game game) => $"loadingbay://mygame/?gameId={game.GameId}";
    public static bool OpenGamePage(Game game, RegistryReader registry, IDialogsFactory dialogs)
    {
        var clientPath = registry.GetClientPath();
        if (!File.Exists(clientPath))
        {
            dialogs.ShowErrorMessage("Can't launch LoadingBay. Installation not found.", $"Error installing {game?.Name}");
            return false;
        }

        try
        {
            Process.Start(GetLoadingBayGamePageUrl(game));
            return true;
        }
        catch (Exception ex)
        {
            dialogs.ShowErrorMessage(ex.Message, "Error opening game page in LoadingBay");
            return false;
        }
    }
}
