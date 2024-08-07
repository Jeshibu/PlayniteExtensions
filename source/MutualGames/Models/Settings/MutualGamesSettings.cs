using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace MutualGames.Models.Settings
{
    public class MutualGamesSettings : ObservableObject
    {
        public ObservableCollection<FriendSourceSettings> FriendSources { get; set; } = new ObservableCollection<FriendSourceSettings>();
        public FriendIdentities FriendIdentities { get; set; } = new FriendIdentities();
        public GameField ImportTo { get; set; } = GameField.Categories;
        public string PropertyNameFormat { get; set; } = "Owned by {0}";
        public CrossLibraryImportMode CrossLibraryImportMode { get; set; } = CrossLibraryImportMode.ImportAll;
        public Guid ImportCrossLibraryFeatureId { get; set; } = Guid.Empty;
        public bool LimitPlayniteLibraryGamesToSamePlatform { get; set; } = true;
    }
}
