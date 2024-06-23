using Playnite.SDK;
using PlayniteExtensions.Metadata.Common;

namespace SteamTagsImporter.BulkImport
{
    public class SteamGamePropertyImportViewModel : GamePropertyImportViewModel
    {
        public RelayCommand<object> CheckSteamCommand
        {
            get => new RelayCommand<object>((a) =>
            {
                foreach (var game in Games)
                {
                    game.IsChecked = game.Game.PluginId == SteamAppIdUtility.SteamLibraryPluginId;
                }
            });
        }
    }
}
