using Playnite.SDK;
using Playnite.SDK.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BigFishLibrary
{
    public class BigFishMetadataProvider : LibraryMetadataProvider
    {
        private readonly BigFishRegistryReader registryReader;

        public BigFishMetadataProvider(BigFishRegistryReader registryReader)
        {
            this.registryReader = registryReader;
        }

        public override GameMetadata GetMetadata(Game game) => GetMetadata(game.GameId, minimal: false);

        public GameMetadata GetMetadata(string sku, bool minimal)
        {
            var registryDetails = registryReader.GetGameDetails(sku);
            var output = new GameMetadata
            {
                GameId = registryDetails.Sku,
                Name = registryDetails.Name,
                Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") },
                InstallDirectory = new FileInfo(registryDetails.ExecutablePath).DirectoryName,
                IsInstalled = true,
            };
            if (!minimal)
            {
                if (File.Exists(registryDetails.Thumbnail))
                    output.Icon = new MetadataFile(registryDetails.Thumbnail);

                string id = new string(registryDetails.Sku.SkipWhile(char.IsLetter).TakeWhile(char.IsNumber).ToArray());
                output.Links = new List<Link> { new Link("Big Fish Store Page", $"https://www.bigfishgames.com/games/{id}/") };
            }
            return output;
        }
    }
}