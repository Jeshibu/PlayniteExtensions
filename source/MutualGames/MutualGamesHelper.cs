using MutualGames.Models.Settings;

namespace MutualGames;

public static class MutualGamesHelper
{
    public static string ExportFileFilter = "Mutual Games export|*.mutualgames";
    public static string GetPropertyName(MutualGamesSettings settings, FriendAccountInfo accountInfo) => GetPropertyName(settings.PropertyNameFormat, accountInfo.Name, accountInfo.Source.ToString());
    public static string GetPropertyName(string format, string friendName, string sourceName) => string.Format(format.Trim(), friendName, sourceName);
}
