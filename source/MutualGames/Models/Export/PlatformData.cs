using Playnite.SDK.Models;
using System;

namespace MutualGames.Models.Export
{
    public class PlatformData
    {
        public Guid Id { get; set; }
        public string SpecificationId {  get; set; }
        public string Name { get; set; }

        public static PlatformData FromPlatform(Platform platform)
        {
            return new PlatformData
            {
                Id = platform.Id,
                SpecificationId = platform.SpecificationId,
                Name = platform.Name,
            };
        }
    }
}
