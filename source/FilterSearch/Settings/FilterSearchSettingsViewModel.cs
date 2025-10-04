using FilterSearch.Helpers;
using Playnite.SDK;
using System.Collections.Generic;
using System.Linq;

namespace FilterSearch.Settings;

public class FilterSearchSettingsViewModel : PluginSettingsViewModel<FilterSearchSettings, FilterSearch>
{
    public FilterSearchSettingsViewModel(FilterSearch plugin, IPlayniteAPI playniteApi) : base(plugin, playniteApi)
    {
        Settings = LoadSavedSettings() ?? new();
        InitializeSearchPropertySettings();
    }

    public Dictionary<FilterActionType, string> ActionTypeOptions => EnumHelper.GetEnumValuesWithDescription<FilterActionType>();

    private void InitializeSearchPropertySettings()
    {
        InitializeSearchPropertySetting(FilterProperty.FilterPreset, true);
        InitializeSearchPropertySetting(FilterProperty.Library);
        InitializeSearchPropertySetting(FilterProperty.AgeRating);
        InitializeSearchPropertySetting(FilterProperty.Category);
        InitializeSearchPropertySetting(FilterProperty.CompletionStatus, true);
        InitializeSearchPropertySetting(FilterProperty.Developer);
        InitializeSearchPropertySetting(FilterProperty.Publisher);
        InitializeSearchPropertySetting(FilterProperty.Feature);
        InitializeSearchPropertySetting(FilterProperty.Genre);
        InitializeSearchPropertySetting(FilterProperty.Platform);
        InitializeSearchPropertySetting(FilterProperty.Region);
        InitializeSearchPropertySetting(FilterProperty.Series);
        InitializeSearchPropertySetting(FilterProperty.Source);
        InitializeSearchPropertySetting(FilterProperty.Tag);
        Settings.SearchProperties.Sort((x, y) => string.CompareOrdinal(x.Property.ToString(), y.Property.ToString()));
    }

    private void InitializeSearchPropertySetting(FilterProperty property,  bool addItemsToGlobalSearch = false)
    {
        if (Settings.SearchProperties.Any(sp => sp.Property == property))
            return;
        
        Settings.SearchProperties.Add(new()
        {
            Property = property,
            EnableSearchContext = true,
            AddItemsToGlobalSearch = addItemsToGlobalSearch,
        });
    }
}