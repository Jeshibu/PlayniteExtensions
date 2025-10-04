using Playnite.SDK;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static OpenCriticMetadata.ImageTypeNames;

namespace OpenCriticMetadata;

public class OpenCriticMetadataSettingsViewModel : PluginSettingsViewModel<OpenCriticMetadataSettings, OpenCriticMetadata>
{
    public OpenCriticMetadataSettingsViewModel(OpenCriticMetadata plugin) : base(plugin, plugin.PlayniteApi)
    {
        Settings = LoadSavedSettings() ?? new OpenCriticMetadataSettings();
        InitializeImageSourceList(Settings.CoverSources, [new(Box, true), new(Square, true), new(Masthead), new(Banner), new(Screenshots)]);
        InitializeImageSourceList(Settings.BackgroundSources, [new(Masthead, true), new(Screenshots, true), new(Banner), new(Box), new(Square)]);
    }

    private void InitializeImageSourceList(ObservableCollection<CheckboxSetting> list, CheckboxSetting[] items)
    {
        var remove = new List<CheckboxSetting>();
        foreach (var existing in list)
            if (!items.Any(i => i.Name == existing.Name))
                remove.Add(existing);

        foreach (var r in remove)
            list.Remove(r);

        foreach (var item in items)
            if (!list.Any(i => i.Name == item.Name))
                list.Add(item);
    }

    public Dictionary<OpenCriticSource, string> CriticSources => new()
    {
        { OpenCriticSource.TopCritics, "Top critics only" },
        { OpenCriticSource.Median, "All critics" },
    };
}
