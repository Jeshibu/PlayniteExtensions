using MobyGamesMetadata.Api;
using PlayniteExtensions.Common;
using PlayniteExtensions.Metadata.Common;
using PlayniteExtensions.Tests.Common;
using Xunit;

namespace MobyGamesMetadata.Tests;

public class GameDetailsTests
{
    [Fact]
    public void GroupsAreParsed()
    {
        var details = GetOblivionRemastered();

        Assert.Single(details.Series);
        Assert.Equal("Elder Scrolls", details.Series[0]);
        
        Assert.Equal(23, details.Tags.Count);
        Assert.Contains("3D Engine: Unreal Engine 5", details.Tags);
        Assert.Contains("Animals: Cats", details.Tags);
        Assert.Contains("Fantasy creatures: Elves", details.Tags);
        Assert.Contains("Fantasy creatures: Goblins", details.Tags);
        Assert.Contains("Fantasy creatures: Golems", details.Tags);
        Assert.Contains("Fantasy creatures: Minotaurs", details.Tags);
        Assert.Contains("Fantasy creatures: Orcs", details.Tags);
        Assert.Contains("Fantasy creatures: Trolls", details.Tags);
        Assert.Contains("Fantasy creatures: Unicorns", details.Tags);
        Assert.Contains("Gameplay feature: Arena fighting", details.Tags);
        Assert.Contains("Gameplay feature: Armor / weapon deterioration", details.Tags);
        Assert.Contains("Gameplay feature: Drowning", details.Tags);
        Assert.Contains("Gameplay feature: Fishing", details.Tags);
        Assert.Contains("Gameplay feature: Horse riding", details.Tags);
        Assert.Contains("Gameplay feature: House ownership", details.Tags);
        Assert.Contains("Gameplay feature: Lock picking", details.Tags);
        Assert.Contains("Middleware: Bink Video", details.Tags);
        Assert.Contains("Middleware: Gamebryo / Lightspeed / NetImmerse", details.Tags);
        Assert.Contains("Protagonist: Female (option)", details.Tags);
        Assert.Contains("Protagonist: Visually customizable character", details.Tags);
        Assert.Contains("Remastered releases", details.Tags);
        Assert.Contains("Sound Engine: Wwise", details.Tags);
        Assert.Contains("Theme: School of magic", details.Tags);
    }

    [Fact]
    public void DescriptionIsParsed()
    {
        var details = GetOblivionRemastered();
        
        Assert.StartsWith("<div", details.Description);
    }

    [Fact]
    public void ReleaseDateIsParsed()
    {
        var details = GetOblivionRemastered();
        
        Assert.Equal(new(2025,4,22), details.ReleaseDate);
    }

    [Fact]
    public void LinksParse()
    {
        var details = GetOblivionRemastered();
        
        Assert.Equal("https://www.mobygames.com/game/240959", details.Url);
        
        Assert.Equal(2, details.Links.Count);
        Assert.Contains(new("Steam", "https://store.steampowered.com/app/2623190/"), details.Links);
        Assert.Contains(new("Visit", "https://elderscrolls.bethesda.net/en-US/oblivion-remastered"), details.Links);
    }

    private GameDetails GetOblivionRemastered()
    {
        var webViewFactory = new FakeWebViewFactory(new()
        {
            { "https://www.mobygames.com/game/240959", "html/game-oblivion-remastered.html" }
        });
        var scraper = new MobyGamesScraper(new PlatformUtility(), webViewFactory);
        return scraper.GetGameDetails(240959);
    }
}
