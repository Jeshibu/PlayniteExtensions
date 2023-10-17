using Xunit;

namespace GiantBombMetadata.Tests
{
    public class HtmlTests
    {
        [Theory]
        [InlineData("//")]
        [InlineData("///")]
        public void BunkPathTurnsIntoBaseUrl(string path)
        {
            string baseUrl = "https://www.giantbomb.com/soldier-of-fortune/3030-22547/";
            string htmlFormat = "<a href=\"{0}\">link</a>";
            string input = string.Format(htmlFormat, path);
            string expected = string.Format(htmlFormat, baseUrl);

            string processed = GiantBombHelper.MakeHtmlUrlsAbsolute(input, baseUrl);

            Assert.Equal(expected, processed);
        }
    }
}
