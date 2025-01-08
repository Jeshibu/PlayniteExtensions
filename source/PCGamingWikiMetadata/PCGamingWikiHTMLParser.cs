using System;
using System.Collections.Generic;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Playnite.SDK;
using System.Globalization;

namespace PCGamingWikiMetadata
{
    public class PCGamingWikiHTMLParser
    {
        private readonly ILogger logger = LogManager.GetLogger();
        private HtmlDocument doc;
        private PCGWGameController gameController;

        public const short UndefinedPlayerCount = -1;

        public PCGamingWikiHTMLParser(string html, PCGWGameController gameController)
        {
            this.doc = new HtmlDocument();
            this.doc.LoadHtml(html);
            this.gameController = gameController;
        }

        public void ApplyGameMetadata()
        {
            ParseInfobox();
            ParseInput();
            ParseCloudSync();
            ParseMultiplayer();
            ParseVideo();
            ParseVR();
        }

        private void RemoveChildElementTypes(HtmlNode node, string type)
        {
            var removeChil = node.SelectNodes(type);

            if (removeChil != null && removeChil.Count > 0)
            {
                node.RemoveChildren(removeChil);
            }
        }

        private void RemoveCitationsFromHTMLNode(HtmlNode node)
        {
            try
            {
                RemoveChildElementTypes(node, ".//sup");
            }
            catch (Exception e)
            {
                logger.Error($"Error removing sup elements: {e.ToString()}");
            }
        }

        private void RemoveSpanFromHTMLNode(HtmlNode node)
        {
            try
            {
                RemoveChildElementTypes(node, ".//span");
            }
            catch (Exception e)
            {
                logger.Error($"Error removing span elements: {e.ToString()}");
            }
        }

        private IList<HtmlNode> SelectTableRowsByClass(string tableId, string rowClass)
        {
            var table = this.doc.DocumentNode.SelectSingleNode($"//table[@id='{tableId}']");

            if (table != null)
            {
                return table.SelectNodes($"//tr[@class='{rowClass}']");
            }

            return new List<HtmlNode>();
        }

        public bool CheckPageRedirect(out string redirectPage)
        {
            HtmlNode node = this.doc.DocumentNode.SelectSingleNode($"//ul[@class='redirectText']");
            redirectPage = null;

            if (node != null)
            {
                redirectPage = node.InnerText;
                return true;
            }

            return false;
        }

        private void ParseVR()
        {
            var rows = SelectTableRowsByClass("table-settings-vr-headsets", "template-infotable-body table-settings-vr-body-row");
            string headset = "";
            string rating = "";

            foreach (HtmlNode row in rows)
            {
                foreach (HtmlNode child in row.SelectNodes(".//th|td"))
                {
                    switch (child.Attributes["class"].Value)
                    {
                        case "table-settings-vr-body-parameter":
                            headset = child.FirstChild.InnerText.Trim();
                            break;
                        case "table-settings-vr-body-rating":
                            rating = child.FirstChild.Attributes["title"].Value;
                            break;
                        case "table-settings-vr-body-notes":
                            break;
                    }
                }

                this.gameController.AddVRFeature(headset, rating);
                headset = "";
                rating = "";
            }
        }

        private void ParseVideo()
        {
            var rows = SelectTableRowsByClass("table-settings-video", "template-infotable-body table-settings-video-body-row");
            string feature = "";
            string rating = "";

            foreach (HtmlNode row in rows)
            {
                foreach (HtmlNode child in row.SelectNodes(".//th|td"))
                {
                    switch (child.Attributes["class"].Value)
                    {
                        case "table-settings-video-body-parameter":
                            feature = child.FirstChild.InnerText.Trim();
                            break;
                        case "table-settings-video-body-rating":
                            rating = child.FirstChild.Attributes["title"].Value;
                            break;
                    }
                }

                this.gameController.AddVideoFeature(feature, rating);
                feature = "";
                rating = "";
            }
        }

        private void ParseMultiplayer()
        {
            var rows = SelectTableRowsByClass("table-network-multiplayer", "template-infotable-body table-network-multiplayer-body-row");
            string networkType = "";
            string rating = "";
            short playerCount = UndefinedPlayerCount;

            foreach (HtmlNode row in rows)
            {
                foreach (HtmlNode child in row.SelectNodes(".//th|td"))
                {
                    switch (child.Attributes["class"].Value)
                    {
                        case "table-network-multiplayer-body-parameter":
                            networkType = child.FirstChild.InnerText;
                            break;
                        case "table-network-multiplayer-body-rating":
                            rating = child.FirstChild.Attributes["title"].Value;
                            break;
                        case "table-network-multiplayer-body-players":
                            Int16.TryParse(child.FirstChild.InnerText, out playerCount);
                            break;
                        case "table-network-multiplayer-body-notes":
                            IList<string> notes = ParseMultiplayerNotes(child);
                            this.gameController.AddMultiplayer(networkType, rating, playerCount, notes);
                            rating = "";
                            networkType = "";
                            playerCount = UndefinedPlayerCount;
                            break;
                    }
                }
            }
        }

        private IList<string> ParseMultiplayerNotes(HtmlNode notes)
        {
            List<string> multiplayerTypes = new List<string>();

            Regex pattern = new Regex(@"class=""table-network-multiplayer-body-notes"">(?<mode1>(Co-op|Versus))?(,)?(&#32;)?(?<mode2>(Co-op|Versus))?<br>");
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
            var rows = SelectTableRowsByClass("table-settings-input", "template-infotable-body table-settings-input-body-row");
            string param = "";

            foreach (HtmlNode row in rows)
            {
                foreach (HtmlNode child in row.SelectNodes(".//th|td"))
                {
                    switch (child.Attributes["class"].Value)
                    {
                        case "table-settings-input-body-parameter":
                            param = child.FirstChild.InnerText;
                            break;
                        case "table-settings-input-body-rating":
                            switch (param)
                            {
                                case "Full controller support":
                                    this.gameController.Game.AddFullControllerSupport(child.FirstChild.Attributes["title"].Value);
                                    break;
                                case "Controller support":
                                    this.gameController.Game.AddControllerSupport(child.FirstChild.Attributes["title"].Value);
                                    break;
                                case "Touchscreen optimised":
                                    this.gameController.Game.AddTouchscreenSupport(child.FirstChild.Attributes["title"].Value);
                                    break;
                                case "PlayStation controllers":
                                    this.gameController.Game.AddPlayStationControllerSupport(child.FirstChild.Attributes["title"].Value);
                                    break;
                                case "PlayStation button prompts":
                                    this.gameController.Game.AddPlayStationButtonPrompts(child.FirstChild.Attributes["title"].Value);
                                    break;
                                case "Light bar support":
                                    this.gameController.Game.AddLightBarSupport(child.FirstChild.Attributes["title"].Value);
                                    break;
                                case "Adaptive trigger support":
                                    this.gameController.Game.AddAdaptiveTriggerSupport(child.FirstChild.Attributes["title"].Value);
                                    break;
                                case "DualSense haptic feedback support":
                                    this.gameController.Game.AddHapticFeedbackSupport(child.FirstChild.Attributes["title"].Value);
                                    break;
                                default:
                                    break;

                            }
                            param = "";
                            break;
                    }
                }
            }
        }

        private void ParseCloudSync()
        {
            var rows = SelectTableRowsByClass("table-cloudsync", "template-infotable-body table-cloudsync-body-row");
            string launcher = "";

            foreach (HtmlNode row in rows)
            {
                foreach (HtmlNode child in row.SelectNodes(".//th|td"))
                {
                    switch (child.Attributes["class"].Value)
                    {
                        case "table-cloudsync-body-system":
                            launcher = child.FirstChild.InnerText;
                            break;
                        case "table-cloudsync-body-rating":
                            this.gameController.AddCloudSaves(launcher, child.FirstChild.Attributes["title"].Value);
                            launcher = "";
                            break;
                    }
                }
            }
        }

        private void ParseInfobox()
        {
            HtmlNode table = this.doc.DocumentNode.SelectSingleNode("//table[@id='infobox-game']");

            if (table == null)
            {
                logger.Error($"Unable to fetch infobox-game table for {this.gameController.Game.Name}");
                return;
            }

            string currentHeader = "";

            foreach (HtmlNode row in table.SelectNodes(".//tr"))
            {
                string key = "";

                foreach (HtmlNode child in row.SelectNodes(".//th|td"))
                {
                    RemoveSpanFromHTMLNode(child);
                    RemoveCitationsFromHTMLNode(child);

                    string text = HtmlEntity.DeEntitize(child.InnerText.Trim());

                    switch (child.Name)
                    {
                        case "th":
                            currentHeader = text;
                            break;

                        case "td":
                            switch (child.Attributes["class"].Value)
                            {
                                case "template-infobox-type":
                                    if (text == "")
                                        break;
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
                                            foreach (HtmlNode data in child.SelectNodes(".//a"))
                                            {
                                                text = HtmlEntity.DeEntitize(data.InnerText.Trim());
                                                this.gameController.AddTaxonomy(key, text);
                                            }
                                            break;
                                        case "Reception":
                                            AddReception(key, child);
                                            break;
                                        case "Release dates":
                                            ApplyReleaseDate(key, text);
                                            break;
                                        case PCGamingWikiType.Taxonomy.Engines:
                                            this.gameController.AddTaxonomy(PCGamingWikiType.Taxonomy.Engines, text);
                                            break;
                                        case "Developers":
                                            this.gameController.AddDeveloper(text);
                                            break;
                                        case "Publishers":
                                            this.gameController.AddPublisher(text);
                                            break;
                                        default:
                                            logger.Debug($"ApplyGameMetadata unknown header {currentHeader}");
                                            break;
                                    }
                                    break;
                            }
                            break;
                    }
                }
            }
        }

        private void AddReception(string aggregator, HtmlNode node)
        {
            int score;

            if (Int32.TryParse(node.SelectNodes(".//a")[0].InnerText, out score))
            {
                this.gameController.Game.AddReception(aggregator, score);
            }
            else
            {
                logger.Error($"Unable to add reception {aggregator} {score}");
            }
        }

        private void ApplyReleaseDate(string platform, string releaseDate)
        {
            DateTime? date = ParseWikiDate(releaseDate);

            if (date == null)
            {
                return;
            }

            this.gameController.Game.AddReleaseDate(platform, ParseWikiDate(releaseDate));
        }

        private DateTime? ParseWikiDate(string dateString)
        {
            if (DateTime.TryParse(dateString, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date) == true)
            {
                return date;
            }
            else
            {
                logger.Error($"Unable to parse date for {this.gameController.Game.Name}: {dateString}");
                return null;
            }
        }

        private void AddLinks(HtmlNode icons)
        {
            string url;
            foreach (var c in icons.ChildNodes)
            {
                url = c.ChildNodes[0].Attributes["href"].Value;
                switch (c.Attributes["Title"].Value)
                {
                    case var title when new Regex(@"^Official site$").IsMatch(title):
                        this.gameController.AddLink(new Playnite.SDK.Models.Link("Official site", url));
                        break;
                    case var title when new Regex(@"GOG Database$").IsMatch(title):
                        this.gameController.AddLink(new Playnite.SDK.Models.Link("GOG Database", url));
                        break;
                    default:
                        string[] linkTitle = c.Attributes["Title"].Value.Split(' ');
                        string titleComp = linkTitle[linkTitle.Length - 1];
                        this.gameController.AddLink(new Playnite.SDK.Models.Link(titleComp, url));
                        break;
                }
            }
        }
    }
}
