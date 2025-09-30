using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace OpenCriticMetadata;

public class OpenCriticMetadataSettings : ObservableObject
{
    public OpenCriticSource CriticScoreSource { get; set; } = OpenCriticSource.TopCritics;
    public int MinimumCriticReviewCount { get; set; } = 1;
    public int MinimumCommunityReviewCount { get; set; } = 20;
    public ObservableCollection<CheckboxSetting> CoverSources { get; set; } = [];
    public ObservableCollection<CheckboxSetting> BackgroundSources { get; set; } = [];
}

public enum OpenCriticSource
{
    TopCritics,
    Median
}

public class CheckboxSetting
{
    public bool Checked { get; set; }
    public string Name { get; set; }

    public CheckboxSetting()    {            }

    public CheckboxSetting(string name, bool isChecked = false)
    {
        Name = name;
        Checked = isChecked;
    }

    public override string ToString()
    {
        var symbol = Checked ? '✅' : '❌';
        return $"{symbol} {Name}";
    }
}

internal static class ImageTypeNames
{
    internal const string Box = "Cover (vertical)";
    internal const string Square = "Cover (square)";
    internal const string Masthead = "Masthead";
    internal const string Banner = "Banner";
    internal const string Screenshots = "Screenshots";
}
