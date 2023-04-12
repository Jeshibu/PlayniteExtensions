using MobyGamesMetadata.Api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MobyGamesMetadata.Tests
{
    public class TestProjectAbuse
    {
        [Fact]
        public void GetAllGroups()
        {
            var client = new MobyGamesApiClient() { ApiKey = null };
            var result = client.GetAllGroups();
            var stringResult = JsonConvert.SerializeObject(result, Formatting.Indented);
        }
    }
}
