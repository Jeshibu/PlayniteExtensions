using PCGamingWikiMetadata;
using Newtonsoft.Json.Linq;
using System.IO;

public class LocalPCGWClient : PCGWClient
{
    public LocalPCGWClient() : base(null, null)
    {
        this.options = new TestMetadataRequestOptions();
        PCGamingWikiMetadataSettings settings = new PCGamingWikiMetadata.PCGamingWikiMetadataSettings();
        this.gameController = new PCGWGameController(settings);
    }

    public LocalPCGWClient(TestMetadataRequestOptions options) : base(null, null)
    {
        this.options = options;
        PCGamingWikiMetadataSettings settings = new PCGamingWikiMetadata.PCGamingWikiMetadataSettings();
        this.gameController = new PCGWGameController(settings);
    }

    public PCGamingWikiMetadataSettings GetSettings()
    {
        return this.gameController.Settings;
    }

    public override void FetchGamePageContent(PCGWGame game)
    {
        this.gameController.Game = game;
        base.FetchGamePageContent(game);
    }
}
