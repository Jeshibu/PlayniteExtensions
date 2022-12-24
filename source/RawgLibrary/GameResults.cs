using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RawgLibrary
{
    public class GameResults
    {
        public string Text { get; set; }
        public List<GameResult> Results { get; set; } = new List<GameResult>();
    }

    public class GameResult
    {
        public GameResult(Game game, string resultText, params GameResultAction[] actions)
        {
            Game = game;
            ResultText = resultText;
            if (actions != null)
                Actions.AddRange(actions);

            foreach (var action in Actions)
            {
                action.Game = game;
            }
        }

        public Game Game { get; set; }
        public string ResultText { get; set; }
        public List<GameResultAction> Actions { get; set; } = new List<GameResultAction>();
    }

    public class GameResultAction
    {
        public GameResultAction(string description, RelayCommand<Game> command)
        {
            Description = description;
            Command = command;
        }

        public string Description { get; set; }
        public RelayCommand<Game> Command { get; set; }
        public Game Game { get; set; }
    }
}
