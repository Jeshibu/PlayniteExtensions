using Linguini.Bundle;
using Linguini.Bundle.Builder;
using Linguini.Shared.Types.Bundle;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using FluentArgs = System.Collections.Generic.Dictionary<string, Linguini.Shared.Types.Bundle.IFluentType>;

namespace itchioBundleTagger;

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

    public string Translate(string id, FluentArgs args = null) => bundle.GetAttrMessage(id, args);

    public IEnumerable<string> GetKeys() => bundle.GetMessageEnumerable();
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
    public string RunOnLibraryUpdate => Translate("setting-run-on-library-update");
    public string ShowInContextMenu => Translate("setting-show-in-context-menu");
    public string AddBundleTagsHeader => Translate("setting-add-bundle-tags-header");

    public string ExecuteTagging => Translate("menu-execute-tagging");
    public string ExecuteTaggingAll => Translate("menu-execute-tagging-all");
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

    public Dictionary<string,string> GetBundleTags()
    {
        var output = new Dictionary<string, string>();
        var keys = base.GetKeys();
        const string bundleTagStart = "tag-bundle-";
        foreach (var key in keys)
        {
            if (!key.StartsWith(bundleTagStart))
                continue;

            var bundleKey = key.Substring(bundleTagStart.Length);
            output.Add(bundleKey, Translate(key));
        }
        return output;
    }
}
