using Playnite.SDK.Models;
using Playnite.SDK;
using System.Collections.Generic;
using System.Linq;
using System;

namespace PlayniteExtensions.Metadata.Common;

public class GamePropertyImportViewModel
{
    public IPlayniteAPI PlayniteAPI { get; set; }

    public GamePropertyImportTargetField[] TargetFieldOptions { get; set; } = new[]
    {
        GamePropertyImportTargetField.Category,
        GamePropertyImportTargetField.Genre,
        GamePropertyImportTargetField.Tag,
        GamePropertyImportTargetField.Feature,
        GamePropertyImportTargetField.Series,
        GamePropertyImportTargetField.Developers,
        GamePropertyImportTargetField.Publishers,
    };

    public GamePropertyImportTargetField TargetField { get; set; }

    public string Name { get; set; }

    public List<PotentialLink> Links { get; set; } = [];

    public List<CheckboxFilter> Filters { get; set; } = [];

    public ICollection<GameCheckboxViewModel> Games { get; set; }
}

public class GameCheckboxViewModel : ObservableObject
{
    private bool isChecked;

    public GameCheckboxViewModel(Game game, GameDetails gameDetails, bool isChecked = true)
    {
        Game = game;
        GameDetails.Add(gameDetails);
        IsChecked = isChecked;
    }

    public Game Game { get; set; }
    public List<GameDetails> GameDetails { get; } = [];
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

public class PotentialLink(string name, Func<GameDetails, string> getUrlMethod, Func<IEnumerable<Link>, string, bool> isAlreadyLinkedMethod = null)
{
    public string Name { get; } = name;
    public bool Checked { get; set; } = true;
    public string GetUrl(GameDetails game) => getUrlMethod(game);
    public virtual bool IsAlreadyLinked(IEnumerable<Link> links, string url)
    {
        if (links == null) return false;

        if (isAlreadyLinkedMethod != null)
            return isAlreadyLinkedMethod(links, url);

        if (string.IsNullOrWhiteSpace(url)) return true;

        return links != null && links.Any(l => url.Equals(l.Url, StringComparison.InvariantCultureIgnoreCase));
    }
}

public class CheckboxFilter
{
    private GamePropertyImportViewModel ViewModel { get; }
    private Func<GameCheckboxViewModel, bool> FilterFunc { get; }

    public CheckboxFilter()
    {
        Cmd = new RelayCommand<object>(_ =>
        {
            foreach (var c in ViewModel.Games)
                c.IsChecked = FilterFunc(c);
        });
    }

    public CheckboxFilter(string text, GamePropertyImportViewModel viewModel, Func<GameCheckboxViewModel, bool> filterFunc) : this()
    {
        Text = text;
        this.ViewModel = viewModel;
        FilterFunc = filterFunc;
    }

    public string Text { get; }
    public RelayCommand<object> Cmd { get; }
}

public enum GamePropertyImportTargetField
{
    Category,
    Genre,
    Tag,
    Feature,
    Series,
    Developers,
    Publishers,
}
