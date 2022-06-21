using Linguini.Bundle;
using Linguini.Bundle.Builder;
using Linguini.Shared.Types.Bundle;
using Linguini.Syntax.Ast;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FluentArgs = System.Collections.Generic.Dictionary<string, Linguini.Shared.Types.Bundle.IFluentType>;

namespace itchioBundleTagger
{
    public abstract class Translator
    {
        public Translator(string language) : this("Localization", language) { }
        public Translator(string localizationFolder, string language)
        {
            LocalizationFolder = localizationFolder;
            SetLanguage(language);
        }

        private FluentBundle bundle;
        private ILogger logger = LogManager.GetLogger();

        public string LocalizationFolder { get; }

        private FluentBundle MakeBundle(string language)
        {
            return LinguiniBuilder.Builder()
                .CultureInfo(new CultureInfo(language))
                .AddResource(ReadFtl(language))
                .SetUseIsolating(false)
                .UncheckedBuild();
        }

        private string ReadFtl(string language)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string lang = language.Replace('_', '-');
            var basePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string path = Path.Combine(basePath, LocalizationFolder, lang + ".ftl");
            logger.Debug("Reading " + path);
            path = Path.GetFullPath(path);

            if (File.Exists(path))
                return File.ReadAllText(path);

            return null;
        }

        private void SetLanguage(string language)
        {
            this.bundle = MakeBundle("en-US");

            if (language == "en-US" || language == "en_US")
                return;

            string target;
            try
            {
                target = ReadFtl(language);
            }
            catch
            {
                // No translation for this language.
                return;
            }
            this.bundle.AddResourceOverriding(target);
        }

        public string Translate(string id, FluentArgs args = null)
        {
            return this.bundle.GetAttrMessage(id, args);
        }
    }

    public class itchIoTranslator : Translator
    {
        public itchIoTranslator(string language) : base(language)
        {
        }

        public string ExtensionName => Translate("extension-name");

        public string TagPrefixSetting => Translate("setting-tag-prefix");
        public string AddFreeTagSetting => Translate("setting-add-free-tag");
        public string AddSteamTagSetting => Translate("setting-add-steam-tag");
        public string AddSteamLinkSetting => Translate("setting-add-steam-link");

        public string ExecuteTagging => Translate("menu-execute-tagging");
        public string RefreshDatabase => Translate("menu-refresh-database");

        public string DatabaseRefreshed => Translate("dialog-database-refreshed");

        public string ProgressStart => Translate("progress-start");
        public string ProgressTagging => Translate("progress-tagging");

        public string ErrorDisplayMessage(Exception ex)
        {
            return Translate("error-display", new FluentArgs { { "error", (FluentString)ex?.Message } });
        }

        public string GetTagName(string tagKey)
        {
            return Translate($"tag-{tagKey}");
        }
    }
}
