using Playnite.SDK;
using System.Diagnostics;
using System.IO;

namespace LegacyGamesLibrary
{
    public class LegacyGamesLibraryClient : LibraryClient
    {
        public LegacyGamesLibraryClient(string exePath, string icon)
        {
            ExePath = exePath;
            _icon = icon;
        }

        private string _icon;

        public override bool IsInstalled => !string.IsNullOrWhiteSpace(ExePath) && File.Exists(ExePath);
        public override string Icon => _icon;

        public string ExePath { get; }

        public override void Open()
        {
            if (IsInstalled)
                try { Process.Start(ExePath); } catch { }
        }
    }
}