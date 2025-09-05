using System;
using System.Collections.Generic;
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
}