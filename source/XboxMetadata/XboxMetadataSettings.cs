using Playnite.SDK;
using System.Collections.Generic;

namespace XboxMetadata
{
    public class XboxMetadataSettings : ObservableObject
    {
        public bool ImportAccessibilityFeatures { get; set; }

        public string Market { get; set; } = "en-us";

        public XboxImageSourceSettings Cover { get; set; }

        public XboxImageSourceSettings Background { get; set; }

        public static XboxMetadataSettings GetInitialSettings()
        {
            return new XboxMetadataSettings()
            {
                Cover = new XboxImageSourceSettings
                {
                    MinWidth = 200,
                    MinHeight = 300,
                    MaxWidth = 600,
                    MaxHeight = 900,
                    AspectRatio = AspectRatio.Vertical,
                    Fields = new List<CheckboxSetting> { new CheckboxSetting(ImageSourceField.Poster, true), new CheckboxSetting(ImageSourceField.BoxArt, true), new CheckboxSetting(ImageSourceField.AppStoreProductImage, true), new CheckboxSetting(ImageSourceField.SuperHeroArt, false), new CheckboxSetting(ImageSourceField.Screenshots, false) },
                },
                Background = new XboxImageSourceSettings
                {
                    MinWidth = 1000,
                    MinHeight = 500,
                    MaxWidth = 2560,
                    MaxHeight = 1440,
                    AspectRatio = AspectRatio.Horizontal,
                    Fields = new List<CheckboxSetting> { new CheckboxSetting(ImageSourceField.SuperHeroArt, true), new CheckboxSetting(ImageSourceField.Screenshots, true), new CheckboxSetting(ImageSourceField.BoxArt, false), new CheckboxSetting(ImageSourceField.Poster, false), new CheckboxSetting(ImageSourceField.AppStoreProductImage, false) },
                },
            };
        }
    }

    public class XboxImageSourceSettings
    {
        public List<CheckboxSetting> Fields { get; set; } = new List<CheckboxSetting>();

        public int MaxHeight { get; set; }
        public int MaxWidth { get; set; }
        public int MinHeight { get; set; }
        public int MinWidth { get; set; }
        public AspectRatio AspectRatio { get; set; }
    }

    public enum AspectRatio
    {
        Any,
        Vertical,
        Horizontal,
        Square,
    }

    public enum ImageSourceField
    {
        BoxArt,
        Poster,
        SuperHeroArt,
        Screenshots,
        AppStoreProductImage,
    }

    public class CheckboxSetting
    {
        public bool Checked { get; set; }
        public ImageSourceField Field { get; set; }

        public CheckboxSetting() { }

        public CheckboxSetting(ImageSourceField field, bool check)
        {
            Field = field;
            Checked = check;
        }

        public override string ToString()
        {
            var symbol = Checked ? "✔" : "❌";
            return $"{symbol} {Field}";
        }
    }

    public class XboxMetadataSettingsViewModel : PluginSettingsViewModel<XboxMetadataSettings, XboxMetadata>
    {
        public XboxMetadataSettingsViewModel(XboxMetadata plugin) : base(plugin, plugin.PlayniteApi)
        {
            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<XboxMetadataSettings>();

            if (savedSettings == null)
            {
                Settings = XboxMetadataSettings.GetInitialSettings();
            }
            else
            {
                Settings = savedSettings;
            }
        }

        public List<AspectRatio> AspectRatios { get; } = new List<AspectRatio> { AspectRatio.Any, AspectRatio.Vertical, AspectRatio.Horizontal, AspectRatio.Square };
    }
}