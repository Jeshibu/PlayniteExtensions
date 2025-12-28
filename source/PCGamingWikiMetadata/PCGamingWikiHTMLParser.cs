using AngleSharp.Dom;
using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using PCGamingWikiType;
using Playnite.SDK;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace PCGamingWikiMetadata;

public class PcGamingWikiHtmlParser(string html, PCGWGameController gameController)
{
    private readonly ILogger _logger = LogManager.GetLogger();
    private readonly IHtmlDocument _doc = new HtmlParser().Parse(html);

    public const short UndefinedPlayerCount = -1;

    public void ApplyGameMetadata()
    {
        ParseInfobox();
        ParseInput();
        ParseCloudSync();
        ParseMultiplayer();
        ParseVideo();
        ParseVr();
        ParseMiddleware();
    }

    private void RemoveChildElementTypes(IElement node, string type)
    {
        var removeChildren = node.QuerySelectorAll(type);
        removeChildren.ForEach(a => a.Remove());

        if (removeChildren?.Any() != true)
            return;

        foreach (var child in removeChildren)
        {
            child.Remove();
        }
    }

    private void RemoveCitationsFromHtmlNode(IElement node)
    {
        try
        {
            RemoveChildElementTypes(node, "sup");
        }
        catch (Exception e)
        {
            _logger.Error($"Error removing sup elements: {e}");
        }
    }

    private void RemoveSpanFromHtmlNode(IElement node)
    {
        try
        {
            RemoveChildElementTypes(node, "span");
        }
        catch (Exception e)
        {
            _logger.Error($"Error removing span elements: {e}");
        }
    }

    private IList<IElement> SelectTableRowsByClass(string tableId, string rowClass)
    {
        var table = _doc.QuerySelector($"table#{tableId}");

        if (table != null)
        {
            return table.QuerySelectorAll($"tr.{rowClass}").ToList();
        }

        return [];
    }

    public bool CheckPageRedirect(out string redirectPage)
    {
        var node = _doc.QuerySelector("ul.redirectText");
        redirectPage = null;

        if (node != null)
        {
            redirectPage = node.InnerHtml.HtmlDecode();
            return true;
        }

        return false;
    }

    private void ParseVr()
    {
        var rows = SelectTableRowsByClass("table-settings-vr-headsets", "table-settings-vr-body-row");

        foreach (IElement row in rows)
        {
            string headset = "";
            string rating = "";

            foreach (IElement child in row.QuerySelectorAll("th, td"))
            {
                switch (child.ClassName)
                {
                    case "table-settings-vr-body-parameter":
                        headset = child.TextContent.HtmlDecode();
                        break;
                    case "table-settings-vr-body-rating":
                        rating = child.FirstElementChild.Attributes["title"].Value;
                        break;
                    case "table-settings-vr-body-notes":
                        break;
                }
            }

            gameController.AddVRFeature(headset, rating);
        }
    }

    private void ParseVideo()
    {
        var rows = SelectTableRowsByClass("table-settings-video", "table-settings-video-body-row");

        foreach (IElement row in rows)
        {
            string feature = "";
            string rating = "";

            foreach (IElement child in row.QuerySelectorAll("th, td"))
            {
                switch (child.Attributes["class"].Value)
                {
                    case "table-settings-video-body-parameter":
                        feature = child.TextContent.HtmlDecode();
                        break;
                    case "table-settings-video-body-rating":
                        rating = child.FirstElementChild.Attributes["title"].Value;
                        break;
                }
            }

            gameController.AddVideoFeature(feature, rating);
        }
    }

    private void ParseMultiplayer()
    {
        var rows = SelectTableRowsByClass("table-network-multiplayer", "table-network-multiplayer-body-row");
        string networkType = "";
        string rating = "";
        short playerCount = UndefinedPlayerCount;

        foreach (IElement row in rows)
        {
            foreach (IElement child in row.QuerySelectorAll("th, td"))
            {
                switch (child.Attributes["class"].Value)
                {
                    case "table-network-multiplayer-body-parameter":
                        networkType = child.TextContent.HtmlDecode();
                        break;
                    case "table-network-multiplayer-body-rating":
                        rating = child.FirstElementChild.Attributes["title"].Value;
                        break;
                    case "table-network-multiplayer-body-players":
                        short.TryParse(child.TextContent.HtmlDecode(), out playerCount);
                        break;
                    case "table-network-multiplayer-body-notes":
                        IList<string> notes = ParseMultiplayerNotes(child);
                        gameController.AddMultiplayer(networkType, rating, playerCount, notes);
                        rating = "";
                        networkType = "";
                        playerCount = UndefinedPlayerCount;
                        break;
                }
            }
        }
    }

    private IList<string> ParseMultiplayerNotes(IElement notes)
    {
        List<string> multiplayerTypes = [];

        Regex pattern = new("""class="table-network-multiplayer-body-notes">(?<mode1>(Co-op|Versus))?(,)?(&#32;)?(?<mode2>(Co-op|Versus))?<br>""");
        Match match = pattern.Match(notes.OuterHtml);

        if (match.Groups["mode1"].Success)
        {
            multiplayerTypes.Add(match.Groups["mode1"].Value);
        }

        if (match.Groups["mode2"].Success)
        {
            multiplayerTypes.Add(match.Groups["mode2"].Value);
        }

        return multiplayerTypes;
    }

    private void ParseInput()
    {
        var rows = SelectTableRowsByClass("table-settings-input", "table-settings-input-body-row");

        string param = "";

        foreach (IElement row in rows)
        {
            foreach (IElement child in row.QuerySelectorAll("th, td"))
            {
                switch (child.ClassName)
                {
                    case "table-settings-input-body-parameter":
                        param = child.TextContent.HtmlDecode();
                        break;
                    case "table-settings-input-body-rating":
                        Action<string> addAction = param switch
                        {
                            "Full controller support" => gameController.Game.AddFullControllerSupport,
                            "Controller support" => gameController.Game.AddControllerSupport,
                            "Touchscreen optimised" => gameController.Game.AddTouchscreenSupport,
                            "PlayStation controllers" => gameController.Game.AddPlayStationControllerSupport,
                            "PlayStation button prompts" => gameController.Game.AddPlayStationButtonPrompts,
                            "Light bar support" => gameController.Game.AddLightBarSupport,
                            "Adaptive trigger support" => gameController.Game.AddAdaptiveTriggerSupport,
                            "DualSense haptic feedback support" => gameController.Game.AddHapticFeedbackSupport,
                            _ => null,
                        };

                        if (addAction != null)
                        {
                            var title = child.FirstElementChild.GetAttribute("title");
                            addAction(title);
                        }

                        param = "";
                        break;
                }
            }
        }
    }

    private void ParseCloudSync()
    {
        var rows = SelectTableRowsByClass("table-cloudsync", "table-cloudsync-body-row");
        string launcher = "";

        foreach (IElement row in rows)
        {
            foreach (IElement child in row.QuerySelectorAll("th, td"))
            {
                switch (child.Attributes["class"].Value)
                {
                    case "table-cloudsync-body-system":
                        launcher = child.TextContent.HtmlDecode();
                        break;
                    case "table-cloudsync-body-rating":
                        gameController.AddCloudSaves(launcher, child.FirstElementChild.Attributes["title"].Value);
                        launcher = "";
                        break;
                }
            }
        }
    }

    private void ParseMiddleware()
    {
        var rows = SelectTableRowsByClass("table-middleware", "table-middleware-body-row");
        foreach (var row in rows)
        {
            var type = row.QuerySelector("th")?.TextContent.HtmlDecode();
            var middleware = row.QuerySelector("td")?.TextContent.HtmlDecode();

            if (string.IsNullOrWhiteSpace(middleware) || string.IsNullOrWhiteSpace(type))
                continue;

            gameController.AddMiddleware(type, middleware);
        }
    }

    private void ParseInfobox()
    {
        var table = _doc.QuerySelector("table#infobox-game");

        if (table == null)
        {
            _logger.Error($"Unable to fetch infobox-game table for {gameController.Game.Name}");
            return;
        }

        string currentHeader = "";

        foreach (IElement row in table.QuerySelectorAll("tr"))
        {
            string key = "";

            foreach (IElement child in row.QuerySelectorAll("th, td"))
            {
                RemoveSpanFromHtmlNode(child);
                RemoveCitationsFromHtmlNode(child);

                string text = child.TextContent.HtmlDecode();

                switch (child.TagName)
                {
                    case "TH":
                        currentHeader = text;
                        break;

                    case "TD":
                        switch (child.ClassName)
                        {
                            case "template-infobox-type":
                                if (!string.IsNullOrEmpty(text))
                                    key = text;

                                break;
                            case "template-infobox-icons":
                                AddLinks(child);
                                break;
                            case "template-infobox-info":
                                if (text == "")
                                    break;
                                switch (currentHeader)
                                {
                                    case "Taxonomy":
                                        foreach (IElement data in child.QuerySelectorAll("a"))
                                        {
                                            text = data.TextContent.HtmlDecode();
                                            gameController.AddTaxonomy(key, text);
                                        }

                                        break;
                                    case "Reception":
                                        AddReception(key, child);
                                        break;
                                    case "Release dates":
                                        ApplyReleaseDate(key, text);
                                        break;
                                    case Taxonomy.Engines:
                                        gameController.AddTaxonomy(Taxonomy.Engines, text);
                                        break;
                                    case "Developers":
                                        gameController.AddDeveloper(text);
                                        break;
                                    case "Publishers":
                                        gameController.AddPublisher(text);
                                        break;
                                    default:
                                        _logger.Debug($"ApplyGameMetadata unknown header {currentHeader}");
                                        break;
                                }

                                break;
                        }

                        break;
                }
            }
        }
    }

    private void AddReception(string aggregator, IElement node)
    {
        if (int.TryParse(node.QuerySelector("a").TextContent.HtmlDecode(), out int score))
        {
            gameController.Game.AddReception(aggregator, score);
        }
        else
        {
            _logger.Error($"Unable to add reception {aggregator} {score}");
        }
    }

    private void ApplyReleaseDate(string platform, string releaseDate)
    {
        DateTime? date = ParseWikiDate(releaseDate);

        if (date == null)
            return;

        gameController.Game.AddReleaseDate(platform, date.Value);
    }

    private DateTime? ParseWikiDate(string dateString)
    {
        if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            return date;

        _logger.Error($"Unable to parse date for {gameController.Game.Name}: {dateString}");
        return null;
    }

    private void AddLinks(IElement icons)
    {
        foreach (var c in icons.Children)
        {
            var title = c.GetAttribute("title");
            var url = c.QuerySelector("a[href]")?.GetAttribute("href");
            if (url == null)
                continue;

            switch (title)
            {
                case "Official site":
                    gameController.AddLink(new("Official site", url));
                    break;
                case var _ when title.EndsWith("GOG Database"):
                    gameController.AddLink(new("GOG Database", url));
                    break;
                default:
                    string[] linkTitle = c.Attributes["title"].Value.Split(' ');
                    string titleComp = linkTitle[linkTitle.Length - 1];
                    gameController.AddLink(new(titleComp, url));
                    break;
            }
        }
    }
}
