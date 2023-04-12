using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlayniteExtensions.Metadata.Common
{
    public class GamePropertyImportViewModel
    {
        public GamePropertyImportTargetField[] TargetFieldOptions { get; set; } = new[]
        {
            GamePropertyImportTargetField.Category,
            GamePropertyImportTargetField.Genre,
            GamePropertyImportTargetField.Tag,
            GamePropertyImportTargetField.Feature,
            GamePropertyImportTargetField.Series,
        };

        public GamePropertyImportTargetField TargetField { get; set; }
        public string Name { get; set; }
        public bool AddLink { get; set; } = true;

        public List<GameCheckboxViewModel> Games { get; set; }

        public RelayCommand<object> CheckAllCommand
        {
            get => new RelayCommand<object>((a) =>
            {
                foreach (var game in Games)
                {
                    game.IsChecked = true;
                }
            });
        }

        public RelayCommand<object> UncheckAllCommand
        {
            get => new RelayCommand<object>((a) =>
            {
                foreach (var game in Games)
                {
                    game.IsChecked = false;
                }
            });
        }
    }

    public class GameCheckboxViewModel : ObservableObject
    {
        private bool isChecked;

        public GameCheckboxViewModel(Game game, GameDetails gameDetails, bool isChecked = true)
        {
            Game = game;
            GameDetails = gameDetails;
            IsChecked = isChecked;
        }

        public Game Game { get; set; }
        public GameDetails GameDetails { get; set; }
        public bool IsChecked { get => isChecked; set => SetValue(ref isChecked, value); }
        public string DisplayName
        {
            get
            {
                if (Game.ReleaseDate == null)
                    return Game.Name;
                else
                    return $"{Game.Name} ({Game.ReleaseDate?.Year})";
            }
        }
    }

    public enum GamePropertyImportTargetField
    {
        Category,
        Genre,
        Tag,
        Feature,
        Series,
    }
}
