using GamesSizeCalculator.Common;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GamesSizeCalculator.PS3;

/// <summary>
/// Only an ISizeCalculator in name for now; installed games cannot get their install size from metadata providers
/// </summary>
public class PS3InstallSizeCalculator(IPlayniteAPI playniteAPI) : ISizeCalculator
{
    public string ServiceName { get; } = "PS3";
    public IPlayniteAPI PlayniteAPI { get; } = playniteAPI;

    public async Task<ulong?> GetInstallSizeAsync(Game game)
    {
        return GetInstallSize(game);
    }

    public ulong? GetInstallSize(Game game)
    {
        var directories = GetRomDirectories(game).Distinct().ToList();
        if (directories.Count == 0)
            return null;

        ulong installSize = 0;
        foreach (var dir in directories)
            installSize += FileSystem.GetDirectorySizeOnDisk(dir);

        return installSize;
    }

    public bool IsPreferredInstallSizeCalculator(Game game) => IsPs3Rom(game);

    public static bool IsPs3Rom(Game game) => game.IsInstalled && HasRoms(game) && HasPS3Platform(game);

    private static bool HasRoms(Game game) => game.Roms?.Any() ?? false;

    private static bool HasPS3Platform(Game game) => game.Platforms?.Any(p => p.SpecificationId == "sony_playstation3") ?? false;

    private IEnumerable<string> GetRomDirectories(Game game)
    {
        foreach (var rom in game.Roms)
        {
            var path = PlayniteAPI.ExpandGameVariables(game, rom.Path);
            var basePath = GetBaseRomDirectoryPath(path);
            if (FileSystem.FileExists(path) && basePath != null)
                yield return basePath;
        }
    }

    private string GetBaseRomDirectoryPath(string path)
    {
        var dir = new FileInfo(path).Directory;
        while (dir != null && dir.Name != "USRDIR")
            dir = dir.Parent;

        while (dir != null && (dir.Name == "USRDIR" || dir.Name == "PS3_GAME"))
            dir = dir.Parent;

        return dir?.FullName;
    }
}
