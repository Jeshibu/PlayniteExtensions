using Newtonsoft.Json.Linq;

namespace PCGamingWikiMetadata;

public class PCGamingWikiJSONParser(JObject content, PCGWGameController gameController)
{
    public void ParseGameDataJson()
    {
        JToken playAnywhere = content.SelectToken("$.parse.links[?(@.* == 'List of Xbox Play Anywhere games')]");

        if (playAnywhere != null)
        {
            gameController.SetXboxPlayAnywhere();
        }
    }

    public string PageHTMLText()
    {
        return content["parse"]["text"]["*"].ToString();
    }
}
