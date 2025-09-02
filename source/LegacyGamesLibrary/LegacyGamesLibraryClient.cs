using Playnite.SDK;
using System.Diagnostics;
using System.IO;

namespace LegacyGamesLibrary;

public class LegacyGamesLibraryClient(string exePath, string icon) : LibraryClient
{
    public override bool IsInstalled => !string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath);
    public override string Icon => icon;

    public override void Open()
    {
        if (IsInstalled)
            try { Process.Start(exePath); } catch { }
    }
}