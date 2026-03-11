using Playnite.SDK.Models;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;

namespace PlayniteExtensions.Metadata.Common;

public class GamePropertyImportViewModel
{
    public GamePropertyImportTargetField[] TargetFieldOptions { get; set; } =
    [
        GamePropertyImportTargetField.Category,
        GamePropertyImportTargetField.Genre,
        GamePropertyImportTargetField.Tag,
        GamePropertyImportTargetField.Feature,
        GamePropertyImportTargetField.Series,
        GamePropertyImportTargetField.Developers,
        GamePropertyImportTargetField.Publishers,
    ];

    public GamePropertyImportTargetField TargetField { get; set; }

    public string Name { get; set; }

    public List<PotentialLink> Links { get; set; } = [];

    public List<CheckboxFilter> Filters { get; set; } = [];

    public ICollection<GameCheckboxViewModel> Games { get; set; }
}

public class MatchedGame
{
    public GameDetails Details { get; set; }
    public Dictionary<ExternalDatabase, int> MatchSourceCounts { get; } = [];

    public MatchedGame(GameDetails details, ExternalDatabase source)
    {
        Details = details;
        AddMatch(source);
    }

    public void AddMatch(ExternalDatabase source)
    {
        if (MatchSourceCounts.ContainsKey(source))
            MatchSourceCounts[source]++;
        else
            MatchSourceCounts.Add(source, 1);
    }

    public string DisplayText
    {
        get
        {
            var sb = new StringBuilder();
            sb.Append(string.Join(" / ", Details.Names));
            sb.Append(" (");
            if (Details.ReleaseDate.HasValue)
                sb.Append(Details.ReleaseDate?.Year).Append(", ");

            sb.Append("matched via: ");
            sb.Append(GetMatchSourceDisplayString());
            sb.Append(')');
            return sb.ToString();
        }
    }

    private string GetMatchSourceDisplayString()
    {
        var sb = new StringBuilder();
        foreach (var kvp in MatchSourceCounts.Where(kvp => kvp.Value > 0))
        {
            if (sb.Length > 0)
                sb.Append(", ");

            if (kvp.Key == ExternalDatabase.None)
                sb.Append("name");
            else
                sb.Append(kvp.Key);

            if (kvp.Value > 1)
                sb.Append(" x").Append(kvp.Value);
        }

        return sb.ToString();
    }
}

public class GameCheckboxViewModel : ObservableObject
{
    public GameCheckboxViewModel(Game game, GameDetails gameDetails, ExternalDatabase source)
    {
        Game = game;
        MatchedGames.Add(new(gameDetails, source));
    }

    public Game Game { get; set; }
    public List<MatchedGame> MatchedGames { get; } = [];

    public bool IsChecked
    {
        get;
        set => SetValue(ref field, value);
    } = true;

    public string DisplayName
    {
        get
        {
            if (Game.ReleaseDate == null)
                return Game.Name;

            return $"{Game.Name} ({Game.ReleaseDate?.Year})";
        }
    }

    public void AddMatchedGame(GameDetails game, ExternalDatabase source)
    {
        var existingMatchedGame = MatchedGames.FirstOrDefault(mg => mg.Details == game);
        if (existingMatchedGame != null)
        {
            existingMatchedGame.AddMatch(source);
            return;
        }

        MatchedGames.Add(new(game, source));
    }

    public List<string> MatchedGameDisplayNames => MatchedGames.Select(g => g.DisplayText).ToList();
}

public class PotentialLink(string name, Func<GameDetails, string> getUrlMethod, Func<IEnumerable<Link>, string, bool> isAlreadyLinkedMethod = null)
{
    public string Name { get; } = name;
    public bool Checked { get; set; } = true;
    public string GetUrl(GameDetails game) => getUrlMethod(game);

    public bool IsAlreadyLinked(IEnumerable<Link> links, string url)
    {
        if (links == null) return false;

        if (isAlreadyLinkedMethod != null)
            return isAlreadyLinkedMethod(links, url);

        return string.IsNullOrWhiteSpace(url) || links.Any(l => url.Equals(l.Url, StringComparison.InvariantCultureIgnoreCase));
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
        ViewModel = viewModel;
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
