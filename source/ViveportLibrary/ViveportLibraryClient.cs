using Playnite.SDK;
using PlayniteExtensions.Common;
using System.Diagnostics;

namespace ViveportLibrary
{
    public class ViveportLibraryClient : LibraryClient
    {
        bool uninstallEntryFetched = false;
        private UninstallProgram viveportUninstallEntry;

        public override bool IsInstalled => ViveportUninstallEntry != null;
        public override string Icon => ViveportLibrary.IconPath;

        private UninstallProgram ViveportUninstallEntry
        {
            get
            {
                if (uninstallEntryFetched) //don't keep trying if it's not installed
                    return viveportUninstallEntry;

                if (viveportUninstallEntry == null)
                {
                    viveportUninstallEntry = Programs.GetUninstallProgram("VIVEPORT");
                    uninstallEntryFetched = true;
                }

                return viveportUninstallEntry;
            }

            set => viveportUninstallEntry = value;
        }

        public override void Open()
        {
            if (!IsInstalled)
                return;

            //as far as I know, "open" isn't a recognized command for this URI scheme, it just opens the client or focuses it after it disregards the parameter
            Process.Start("viveport://open");
        }
    }
}