using Playnite.SDK;
using System.Diagnostics;
using System.IO;

namespace LegacyGamesLibrary;

public class LegacyGamesLibraryClient(string exePath, string icon) : LibraryClient
{
    public override bool IsInstalled => !string.IsNullOrWhiteSpace(ExePath) && File.Exists(ExePath);
    public override string Icon => icon;

    public string ExePath { get; } = exePath;

    public override void Open()
    {
        if (IsInstalled)
            try { Process.Start(ExePath); } catch { }
    }
}