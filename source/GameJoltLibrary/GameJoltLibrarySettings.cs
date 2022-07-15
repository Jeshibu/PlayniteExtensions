using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GameJoltLibrary
{
    public class GameJoltLibrarySettings : ObservableObject
    {
        private string username;
        //private List<Cookie> cookies = new List<Cookie>();
        private bool importUninstalledGames = false;
        private bool importInstalledGames = true;


        public string Username { get => username; set => SetValue(ref username, value); }

        //public List<Cookie> Cookies { get => cookies; set => SetValue(ref cookies, value); }

        public bool ImportUninstalledGames { get => importUninstalledGames; set => SetValue(ref importUninstalledGames, value); }
        public bool ImportInstalledGames { get => importInstalledGames; set => SetValue(ref importInstalledGames, value); }
    }

    public class GameJoltLibrarySettingsViewModel : PluginSettingsViewModel<GameJoltLibrarySettings, GameJoltLibrary>
    {
        public GameJoltLibrarySettingsViewModel(GameJoltLibrary plugin) : base(plugin, plugin.PlayniteApi)
        {
            // Load saved settings or instantiate new settings if there are none yet.
            Settings = plugin.LoadPluginSettings<GameJoltLibrarySettings>() ?? new GameJoltLibrarySettings();
        }

        public override bool VerifySettings(out List<string> errors)
        {
            errors = new List<string>();
            if (Settings.ImportUninstalledGames && string.IsNullOrWhiteSpace(Settings.Username))
            {
                errors.Add("Username required for importing uninstalled games");
            }
            return !errors.Any();
        }
    }
}