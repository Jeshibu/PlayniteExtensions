using System.Collections.Generic;

namespace MutualGames.Models.Export
{
    public class ExportFilePromptViewModel
    {
        public Dictionary<ExportGamesMode, string> Modes { get; } = new Dictionary<ExportGamesMode, string>()
        {
            { ExportGamesMode.Filtered, "Only currently visible games (filtered)" },
            { ExportGamesMode.AllExcludeHidden, "All games (exclude hidden)" },
            { ExportGamesMode.AllIncludeHidden, "All games (include hidden)" },
        };

        public ExportGamesMode Mode { get; set; }
    }
}
