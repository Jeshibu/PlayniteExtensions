using System;
using System.Collections.Generic;
using System.Linq;

namespace FilterSearch.Helpers;

public static class EnumHelper
{
    public static T[] GetEnumValues<T>() where T : Enum => Enum.GetValues(typeof(T)).Cast<T>().ToArray();
    public static Dictionary<T, string> GetEnumValuesWithDescription<T>() where T : Enum => GetEnumValues<T>().ToDictionary(e => e, e => e.GetDescription());
}