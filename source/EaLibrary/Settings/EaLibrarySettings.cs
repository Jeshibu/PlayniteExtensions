namespace EaLibrary.Settings;

public class EaLibrarySettings
{
    public int Version { get; set; }
    public bool ConnectAccount { get; set; } = true;
    public bool ImportInstalledGames { get; set; } = true;
    public bool ImportUninstalledGames { get; set; } = true;
}
