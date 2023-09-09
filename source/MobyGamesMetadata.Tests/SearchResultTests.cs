using MobyGamesMetadata.Api;
using Xunit;

namespace MobyGamesMetadata.Tests
{
    public class SearchResultTests
    {
        [Fact]
        public void NumericTitleUrlParsesCorrectId()
        {
            var sr = new SearchResult();
            sr.SetUrlAndId("https://www.mobygames.com/game/86550/640/");
            Assert.Equal(86550, sr.Id);
        }
    }
}
