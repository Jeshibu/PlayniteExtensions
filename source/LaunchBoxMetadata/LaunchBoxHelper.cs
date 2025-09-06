using System;
using System.Linq;

namespace LaunchBoxMetadata;

public static class LaunchBoxHelper
{
    public static string[] SplitLaunchBox(this string str)
    {
        if (str == null)
            return [];

        return str
            .Split([';'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrEmpty(x))
            .ToArray();
    }
    
    /// <summary>
    /// This should always be a sequence of characters that is in no game (alternate) name
    /// Obvious things like ; or , are in quite a few game names, thus 4 pipe symbols
    /// </summary>
    public const string AliasSeparator = "||||";

    public static string[] SplitAliases(this string str)
    {
        if (str == null)
            return [];
        
        return str.Split([AliasSeparator],  StringSplitOptions.None);
    }
}