using GiantBombMetadata.Api;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GiantBombMetadata
{
    public class GamePropertyImportViewModel
    {
        public GamePropertyImportTargetField[] TargetFieldOptions { get; set; } = new[]
        {
            GamePropertyImportTargetField.Category,
            GamePropertyImportTargetField.Genre,
            GamePropertyImportTargetField.Tag,
            GamePropertyImportTargetField.Feature,
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

        public GameCheckboxViewModel(Game game, GiantBombObject gbObj, bool isChecked = true)
        {
            Game = game;
            GiantBombData = gbObj;
            IsChecked = isChecked;
        }

        public Game Game { get; set; }
        public GiantBombObject GiantBombData { get; set; }
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
        Feature
    }
}
