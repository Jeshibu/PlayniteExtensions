namespace MutualGames.Models.Settings;

public enum FriendSource
{
    Steam,
    EA,
    GOG
}

public enum CrossLibraryImportMode
{
    SameLibraryOnly,
    ImportAll,
    ImportAllWithFeature,
}

public enum AuthStatus
{
    Ok,
    Checking,
    AuthRequired,
    Failed
}
