//using Playnite.SDK.Models;
//using PlayniteExtensions.Metadata.Common;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading;
//using System.Threading.Tasks;
//using Xunit;

//namespace PlayniteExtensions.Common.Tests
//{
//    public class GameMatchingHelperTests
//    {
//        private static GameMatchingHelper Setup()
//        {
//            return new GameMatchingHelper(new MobyGamesIdUtility(), 2);
//        }

//        [Theory]
//        [InlineData("City Legends: The Curse of the Crimson Shadow Collector's Edition", "City Legends: The Curse of the Crimson Shadow (Collector's Edition)")]
//        [InlineData("Apothecarium: The Renaissance of Evil Collector's Edition", "Apothecarium: The Renaissance of Evil (Premium Edition)")]
//        public void MatchesByName(string libraryName, string externalName)
//        {
//            var gameMatchingHelper = Setup();

//            var libraryGame = new Game(libraryName);

//            gameMatchingHelper.Prepare(new[] { libraryGame }, CancellationToken.None);

//            gameMatchingHelper.TryGetGamesByName(externalName, out var result);

//            Assert.NotNull(result);
//            Assert.Single(result, libraryGame);
//        }

//        [Fact]
//        public void DoesNotMatchWhenPlatformsDoNotOverlap()
//        {

//        }
//    }
//}
