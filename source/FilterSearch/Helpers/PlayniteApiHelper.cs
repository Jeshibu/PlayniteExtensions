using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.ComponentModel;
using System.Reflection;

namespace FilterSearch.Helpers;

public static class PlayniteApiHelper
{
    public static FilterPreset GetFilterPreset(this IMainViewAPI mainViewApi, FilterPresetSettings filterSettings = null)
    {
        return new()
        {
            Settings = filterSettings ?? mainViewApi.GetCurrentFilterSettings(),
            GroupingOrder = mainViewApi.Grouping,
            SortingOrder = mainViewApi.SortOrder,
            SortingOrderDirection = mainViewApi.SortOrderDirection
        };
    } 
    
    public static string GetDescription(this Enum source)
    {
        FieldInfo field = source.GetType().GetField(source.ToString());
        if (field == null)
        {
            return string.Empty;
        }

        var attributes = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);
        if (attributes.Length > 0)
        {
            var desc = attributes[0].Description;
            if (desc.StartsWith("LOC", StringComparison.Ordinal))
            {
                return ResourceProvider.GetString(desc);
            }
            else
            {
                return attributes[0].Description;
            }
        }
        else
        {
            return source.ToString();
        }
    }
}