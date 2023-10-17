using Newtonsoft.Json;
using Playnite.SDK.Models;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ViveportLibrary.Api;
using Xunit;

namespace ViveportLibrary.Tests
{
    public class ViveportMetadataProviderTests
    {
        private ViveportMetadataProvider Setup(string filename, bool headsetsAsPlatforms = true)
        {
            return new ViveportMetadataProvider(new FakeViveportApiClient(filename), new ViveportLibrarySettings { ImportHeadsetsAsPlatforms = headsetsAsPlatforms });
        }

        private GameMetadata GetStride()
        {
            var metadataProvider = Setup("stride.json");
            var gameMetadata = metadataProvider.GetMetadata(new Game { GameId = "d4c9907a-8e53-4605-830d-003916f6bd54" });

            return gameMetadata;
        }

        [Fact]
        public void MetadataNameIsStride()
        {
            var metadata = GetStride();

            Assert.Equal("STRIDE", metadata.Name);
        }

        [Fact]
        public void MetadataDeveloperAndPublisherIsJoyWay()
        {
            var metadata = GetStride();

            Assert.Equal("Joy Way", metadata.Developers.Cast<MetadataNameProperty>().Single().Name);
            Assert.Equal("Joy Way", metadata.Publishers.Cast<MetadataNameProperty>().Single().Name);
        }

        //[Fact]
        //public void RunForReal()
        //{
        //    var metadataProvider = new ViveportMetadataProvider(new ViveportApiClient(), new ViveportLibrarySettings { ImportHeadsetsAsPlatforms = true });
        //    var gameMetadata = metadataProvider.GetMetadata(new Game { GameId = "d4c9907a-8e53-4605-830d-003916f6bd54" });
        //    Assert.Equal("STRIDE", gameMetadata.Name);
        //}
    }

    public class FakeViveportApiClient : IViveportApiClient
    {
        private readonly string filename;

        public FakeViveportApiClient(string filename)
        {
            this.filename = filename;
        }

        public Task<GetCustomAttributeResponseRoot> GetAttributesAsync(CancellationToken cancellationToken = default)
        {
            var content = File.ReadAllText("custom_attributes.json");
            var output = JsonConvert.DeserializeObject<GetCustomAttributeResponseRoot>(content);
            return Task.FromResult(output);
        }

        public Task<CmsAppDetailsResponse> GetGameDetailsAsync(string appId, CancellationToken cancellationToken = default)
        {
            var content = File.ReadAllText(filename);
            var output = JsonConvert.DeserializeObject<CmsAppDetailsResponse>(content);
            return Task.FromResult(output);
        }
    }
}
