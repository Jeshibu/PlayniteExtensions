using SteamTagsImporter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace SteamTagsImporterTests
{
    public class SteamAppIdUtilityTests
    {
        [Fact]
        public void NullLinkCollectionDoesNotThrowException()
        {
            var game = new Playnite.SDK.Models.Game("THOR.N");
            var util = new SteamAppIdUtility();
            var id = util.GetSteamGameId(game);
        }
    }
}
