using Playnite.SDK;
using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteamActions
{
    public class SteamActionsSettings : ObservableObject
    {
        private string languageKey = "english";

        public string LanguageKey { get => languageKey; set => SetValue(ref languageKey, value); }
        public bool ProvideControllerConfigAction { get; set; } = true;
    }

    public class SteamActionsSettingsViewModel : PluginSettingsViewModel<SteamActionsSettings, SteamActions>, ISettings
    {
        public SteamActionsSettingsViewModel(SteamActions plugin) : base(plugin, plugin.PlayniteApi)
        {
            // Load saved settings.
            var savedSettings = plugin.LoadPluginSettings<SteamActionsSettings>();

            // LoadPluginSettings returns null if no saved data is available.
            Settings = savedSettings ?? new SteamActionsSettings() { LanguageKey = GetSteamLanguageForCurrentPlayniteLanguage() };
        }

        public Dictionary<string, string> Languages { get; } = new Dictionary<string, string>
        {
            {"schinese","简体中文 (Simplified Chinese)"},
            {"tchinese","繁體中文 (Traditional Chinese)"},
            {"japanese","日本語 (Japanese)"},
            {"koreana","한국어 (Korean)"},
            {"thai","ไทย (Thai)"},
            {"bulgarian","Български (Bulgarian)"},
            {"czech","Čeština (Czech)"},
            {"danish","Dansk (Danish)"},
            {"german","Deutsch (German)"},
            {"english","English"},
            {"spanish","Español - España (Spanish - Spain)"},
            {"latam","Español - Latinoamérica (Spanish - Latin America)"},
            {"greek","Ελληνικά (Greek)"},
            {"french","Français (French)"},
            {"italian","Italiano (Italian)"},
            {"hungarian","Magyar (Hungarian)"},
            {"dutch","Nederlands (Dutch)"},
            {"norwegian","Norsk (Norwegian)"},
            {"polish","Polski (Polish)"},
            {"portuguese","Português (Portuguese)"},
            {"brazilian","Português - Brasil (Portuguese - Brazil)"},
            {"romanian","Română (Romanian)"},
            {"russian","Русский (Russian)"},
            {"finnish","Suomi (Finnish)"},
            {"swedish","Svenska (Swedish)"},
            {"turkish","Türkçe (Turkish)"},
            {"vietnamese","Tiếng Việt (Vietnamese)"},
            {"ukrainian","Українська (Ukrainian)"},
        };

        private string GetSteamLanguageForCurrentPlayniteLanguage()
        {
            switch (PlayniteApi.ApplicationSettings.Language)
            {
                case "cs_CZ": return "czech";
                case "da_DK": return "danish";
                case "de_DE": return "german";
                case "el_GR": return "greek";
                case "es_ES": return "spanish";
                case "fi_FI": return "finnish";
                case "fr_FR": return "french";
                case "hu_HU": return "hungarian";
                case "it_IT": return "italian";
                case "ja_JP": return "japanese";
                case "ko_KR": return "korean";
                case "nl_NL": return "dutch";
                case "no_NO": return "norwegian";
                case "pl_PL": return "polish";
                case "pt_BR": return "brazilian";
                case "pt_PT": return "portuguese";
                case "ro_RO": return "romanian";
                case "ru_RU": return "russian";
                case "sv_SE": return "swedish";
                case "tr_TR": return "turkish";
                case "uk_UA": return "ukrainian";
                case "vi_VN": return "vietnamese";
                case "zh_CN":
                case "zh_TW": return "schinese";
                case "en_US":
                default: return "english";
                    //no cultures for latam, thai, bulgarian, tchinese
            }
        }
    }
}