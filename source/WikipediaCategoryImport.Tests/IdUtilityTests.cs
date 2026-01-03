using PlayniteExtensions.Common;

namespace WikipediaCategoryImport.Tests;

public class IdUtilityTests
{
    [Fact]
    public void RiseOfTheRonin()
    {
        var idUtility = new WikipediaIdUtility();
        var escapedId = idUtility.GetIdFromUrl("https://en.wikipedia.org/wiki/Rise_of_the_R%C5%8Dnin");
        var unescapedId = idUtility.GetIdFromUrl("https://en.wikipedia.org/wiki/Rise_of_the_Rōnin");
        Assert.Equal(escapedId, unescapedId);
    }
}
