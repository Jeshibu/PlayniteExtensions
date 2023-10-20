using MobyGamesMetadata.Api;
using RestSharp;
using System.Linq;
using System.Threading;
using Xunit;
using System.IO;

namespace MobyGamesMetadata.Tests
{
    public class GameNameEncodingTests
    {
        public MobyGamesApiClient SetUpClient(string filename)
        {
            return new MobyGamesApiClient((RestRequest request, CancellationToken cancellationToken) =>
            {
                return new RestResponse { Content = File.ReadAllText(filename), StatusCode = System.Net.HttpStatusCode.OK };
            });
        }

        [Fact]
        public void GameSearchResultNamesAreDecoded()
        {
            var client = SetUpClient("ac2search.json");
            var result = client.SearchGames("assassin's creed ii");
            Assert.NotNull(result);
            Assert.Equal("Assassin's Creed II", result.First().Title);
        }

        [Fact]
        public void GameDetailTitlesAreDecoded()
        {
            var client = SetUpClient("ac2.json");
            var result = client.GetMobyGame(43958);
            Assert.NotNull(result);
            Assert.Equal("Assassin's Creed II", result.Title);
        }
    }
}
