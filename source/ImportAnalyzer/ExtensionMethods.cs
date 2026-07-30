using System;
using System.Collections.Generic;
using System.Linq;

namespace ImportAnalyzer;

public static class ExtensionMethods
{
    public static Dictionary<string, List<T>> ToGroupedDictionary<T>(this IEnumerable<T> collection, Func<T, string> keySelector)
    {
        return collection.GroupBy(keySelector).ToDictionary(i => i.Key, i => i.ToList());
    }
}
