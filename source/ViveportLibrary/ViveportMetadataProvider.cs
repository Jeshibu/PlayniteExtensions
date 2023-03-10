using Playnite.SDK;
using Playnite.SDK.Models;
using PlayniteExtensions.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ViveportLibrary.Api;

namespace ViveportLibrary
{
    public class ViveportMetadataProvider : LibraryMetadataProvider
    {
        private readonly IViveportApiClient viveportApiClient;
        private readonly ViveportLibrarySettings settings;
        private readonly ILogger logger = LogManager.GetLogger();

        public ViveportMetadataProvider(IViveportApiClient viveportApiClient, ViveportLibrarySettings settings)
        {
            this.viveportApiClient = viveportApiClient;
            this.settings = settings;
        }

        public override GameMetadata GetMetadata(Game game)
        {
            var detailsTask = viveportApiClient.GetGameDetailsAsync(game.GameId);
            var customAttributesTask = viveportApiClient.GetAttributesAsync();

            Task.WaitAll(detailsTask, customAttributesTask);

            var cmsDetails = detailsTask.Result?.Contents.SingleOrDefault();
            var appDetails = cmsDetails?.Apps?.FirstOrDefault(a => a.Id == game.GameId);
            var customAttributes = customAttributesTask.Result?.Data?.CustomAttributeMetadata?.Items;

            if (appDetails == null || customAttributes == null)
            {
                logger.Error($"No details or custom attributes found for {game.GameId}");
                logger.Error($"Details response: {JsonSerializer.Serialize(detailsTask.Result)}");
                logger.Error($"Attributes response: {JsonSerializer.Serialize(customAttributesTask.Result)}");
                return null;
            }

            var metadata = new GameMetadata
            {
                Name = appDetails.Title ?? cmsDetails.Title,
                Description = (appDetails.Desc ?? cmsDetails.Desc)?.Replace("\n", "<br>\n"),
                Source = new MetadataNameProperty("Viveport"),
                Genres = GetCustomAttributeMetadataProperties(appDetails.Genres, customAttributes, "genres", opt => opt.Value),
                InstallSize = GetInstallSize(appDetails),
                AgeRatings = GetCustomAttributeMetadataProperties(new[] { appDetails.ContentRating.ToString() }, customAttributes, "content_rating", opt => opt.AdminLabel),
                ReleaseDate = new ReleaseDate(GetDateFromMilliseconds(appDetails.ReleaseTimeMilliseconds)),
                Version = appDetails.VersionName,
                Links = new List<Link> { new Link("Viveport Store Page", $"https://www.viveport.com/apps/{game.GameId}") },
            };

            var coverUrl = GetCoverUrl(appDetails);
            if (coverUrl != null)
                metadata.CoverImage = new MetadataFile(coverUrl);

            var developer = appDetails.DeveloperDisplayName?.TrimCompanyForms();
            if (!string.IsNullOrWhiteSpace(developer))
                metadata.Developers = new HashSet<MetadataProperty> { new MetadataNameProperty(developer.TrimCompanyForms()) };

            var publisher = appDetails.Publisher?.TrimCompanyForms();
            if (!string.IsNullOrWhiteSpace(publisher))
                metadata.Publishers = new HashSet<MetadataProperty> { new MetadataNameProperty(publisher.TrimCompanyForms()) };

            #region platforms
            if (settings.ImportHeadsetsAsPlatforms)
                metadata.Platforms = GetCustomAttributeMetadataProperties(appDetails.HardwareMatrix?.Headsets, customAttributes, "headsets", opt => opt.AdminLabel);
            else
                metadata.Platforms = new HashSet<MetadataProperty>();

            if (appDetails.SystemRequirements?.OS != null)
            {
                foreach (var os in appDetails.SystemRequirements.OS)
                {
                    if (os.StartsWith("win", StringComparison.InvariantCultureIgnoreCase))
                        metadata.Platforms.Add(new MetadataSpecProperty("pc_windows"));
                    else
                        logger.Warn($"Unknown OS: {os}");
                }
            }
            #endregion platforms

            var biggestImage = appDetails.Gallery
                                         .Where(i => i.MediaType == 0) //no videos
                                         .OrderByDescending(i => i.Width * i.Height)
                                         .FirstOrDefault();
            metadata.BackgroundImage = new MetadataFile(biggestImage?.Url);

            List<string> features = GetCustomAttributeLabels(appDetails.PlayerNum, customAttributes, "player_num", opt => opt.AdminLabel).Select(s => s.Replace("Singleplayer", "Single-player")).ToList();
            if (settings.ImportInputMethodsAsFeatures)
                features.AddRange(GetCustomAttributeLabels(appDetails.InputMethods, customAttributes, "input_methods", opt => opt.AdminLabel));
            features.AddRange(GetCustomAttributeLabels(appDetails.PlayArea, customAttributes, "play_area", opt => opt.AdminLabel).Select(s => $"VR {s}"));
            features.Add("VR");

            metadata.Features = new HashSet<MetadataProperty>(features.Select(f => new MetadataNameProperty(f)));

            return metadata;
        }

        private string GetCoverUrl(ViveportApp appDetails)
        {
            var verticalCover = appDetails.Cloud?.Objs.FirstOrDefault(o => o.Type == "portrait_image");
            switch (settings.CoverPreference)
            {
                case CoverPreference.None:
                    return null;
                case CoverPreference.VerticalOrSquare:
                    return verticalCover?.Url ?? appDetails.Thumbnails?.Square?.Url;
                case CoverPreference.VerticalOrBust:
                    return verticalCover?.Url;
                case CoverPreference.Square:
                    return appDetails.Thumbnails?.Square?.Url;
                case CoverPreference.Horizontal:
                    return appDetails.Thumbnails?.Medium?.Url;
                default:
                    return null;
            }
        }

        private IEnumerable<string> GetCustomAttributeLabels<T>(IEnumerable<T> values, CustomAttributeMetadataItem[] items, string attributeCodeName, Func<AttributeOption, T> matchSelector)
        {
            var relevantItem = items?.SingleOrDefault(i => i.AttributeCode == attributeCodeName);
            if (relevantItem == null)
            {
                logger.Error($"Attribute {attributeCodeName} not found");
                yield break;
            }

            foreach (var val in values)
            {
                foreach (var opt in relevantItem.AttributeOptions)
                {
                    var matchSelectorResult = matchSelector(opt);
                    if (matchSelectorResult.Equals(val))
                    {
                        yield return opt.Label;
                        break;
                    }
                }
            }
        }

        private HashSet<MetadataProperty> GetCustomAttributeMetadataProperties<T>(IEnumerable<T> values, CustomAttributeMetadataItem[] items, string attributeCodeName, Func<AttributeOption, T> matchSelector)
        {
            return GetCustomAttributeLabels(values, items, attributeCodeName, matchSelector).Select(s => new MetadataNameProperty(s)).ToList().NullIfEmpty()?.ToHashSet<MetadataProperty>();
        }

        private ulong GetInstallSize(ViveportApp appDetails)
        {
            var otherRequirements = appDetails?.SystemRequirements?.Others;
            if (otherRequirements == null)
                return 0;

            if (!ulong.TryParse(otherRequirements.DiskSpace, out var diskspace))
                return 0;

            var power = GetPowerOf1024FromUnit(otherRequirements.DiskSpaceUnit);

            return diskspace * (ulong)Math.Pow(1024, power);
        }

        private int GetPowerOf1024FromUnit(string unit)
        {
            switch (unit)
            {
                case "B": return 0;
                case "KB": return 1;
                case "MB": return 2;
                case "GB": return 3;
                case "TB": return 4;
                default: return 0;
            }
        }

        private static DateTime GetDateFromMilliseconds(long milliseconds)
        {
            return new DateTime(1970, 1, 1).AddMilliseconds(milliseconds);
        }
        private static DateTime GetDateFromSeconds(long seconds)
        {
            return new DateTime(1970, 1, 1).AddSeconds(seconds);
        }
    }
}
