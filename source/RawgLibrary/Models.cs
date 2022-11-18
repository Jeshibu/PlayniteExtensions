using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RawgLibrary
{
    public class Range
    {
        public int Min;
        public int Max;

        [DontSerialize]
        public string MinString { get => Min.ToString(); set => int.TryParse(value, out Min); }

        [DontSerialize]
        public string MaxString { get => Max.ToString(); set => int.TryParse(value, out Max); }
    }

    public class RawgToPlayniteStatus
    {
        public string Id;
        public string Description;
        public Guid PlayniteCompletionStatusId;

        public RawgToPlayniteStatus(string id, string description, Guid playniteCompletionStatusId)
        {
            Id = id;
            Description = description;
            PlayniteCompletionStatusId = playniteCompletionStatusId;
        }
    }

    public class RawgToPlayniteRating
    {
        public int Id;
        public string Description;
        public int PlayniteRating;
        public string PlayniteRatingString
        {
            get => PlayniteRating.ToString();
            set
            {
                if (!int.TryParse(value, out PlayniteRating))
                    PlayniteRating = -1;
            }
        }

        public RawgToPlayniteRating(int id, string description, int playniteRating)
        {
            Id = id;
            Description = description;
            PlayniteRating = playniteRating;
        }
    }

    public class PlayniteToRawgStatus
    {
        public PlayniteToRawgStatus(CompletionStatus playniteCompletionStatus, string rawgStatusId)
        {
            PlayniteCompletionStatus = playniteCompletionStatus;
            RawgStatusId = rawgStatusId;
        }

        public CompletionStatus PlayniteCompletionStatus;
        public string RawgStatusId;
    }

    public class PlayniteToRawgRating
    {
        public PlayniteToRawgRating(int id, string description, Range range)
        {
            Id = id;
            Description = description;
            Range = range;
        }
        public int Id;
        public string Description;
        public Range Range;
    }

    public static class RawgMapping
    {
        private static ILogger logger = LogManager.GetLogger();

        //"Not Played", "Played", "Beaten", "Completed", "Playing", "Abandoned", "On Hold", "Plan to Play"
        public static Dictionary<string, string> RawgCompletionStatuses = new Dictionary<string, string>
        {
            { "owned", "Uncategorized" },
            { "playing", "Currently Playing" },
            { "beaten", "Completed" },
            { "dropped", "Played" },
            { "yet", "Not played" },
        };

        private static Dictionary<string, string> RawgToPlayniteStatusDefaults = new Dictionary<string, string>
        {
            { "owned", "Not Played" },
            { "playing", "Playing" },
            { "beaten", "Beaten" },
            { "dropped", "Abandoned" },
            { "yet", "Not Played" },
        };

        public static Dictionary<int, string> RawgRatings = new Dictionary<int, string>
        {
            { 1, "skip" },
            { 3, "meh" },
            { 4, "recommended" },
            { 5, "excellent" },
        };

        private static Dictionary<string, string> PlayniteToRawgStatusDefaults = new Dictionary<string, string>
        {
            { "Not Played", "yet" },
            { "Played", "owned" },
            { "Beaten", "beaten" },
            { "Completed", "beaten" },
            { "Playing", "playing" },
            { "Abandoned", "dropped" },
            { "On Hold", "owned" },
            { "Plan to Play", "yet" },
        };

        public static IEnumerable<RawgToPlayniteStatus> GetRawgToPlayniteCompletionStatuses(IPlayniteAPI playniteAPI, RawgLibrarySettings settings)
        {
            var playniteStatuses = playniteAPI.Database.CompletionStatuses;

            foreach (var cs in RawgCompletionStatuses)
            {
                CompletionStatus playniteStatus = null;

                if (settings?.RawgToPlayniteStatuses != null && settings.RawgToPlayniteStatuses.TryGetValue(cs.Key, out Guid? id) && id.HasValue)
                    playniteStatus = playniteStatuses[id.Value];

                if (playniteStatus == null && RawgToPlayniteStatusDefaults.TryGetValue(cs.Key, out string playniteStatusName))
                    playniteStatus = playniteStatuses.FirstOrDefault(s => s.Name.Equals(playniteStatusName, StringComparison.InvariantCultureIgnoreCase));

                if (playniteStatus == null)
                    playniteStatus = playniteStatuses.FirstOrDefault();

                logger.Trace($"Completion statuses: {playniteStatuses?.Count}, selected completion status: {playniteStatus?.Name}");

                yield return new RawgToPlayniteStatus(cs.Key, cs.Value, playniteStatus.Id);
            }
        }

        public static IEnumerable<RawgToPlayniteRating> GetRawgToPlayniteRatings(RawgLibrarySettings settings)
        {
            foreach (var r in RawgRatings)
            {
                int rating;
                if (settings?.RawgToPlayniteRatings == null || !settings.RawgToPlayniteRatings.TryGetValue(r.Key, out rating))
                    rating = r.Key * 20;

                yield return new RawgToPlayniteRating(r.Key, r.Value, rating);
            }
        }

        public static IEnumerable<PlayniteToRawgStatus> GetPlayniteToRawgStatuses(IPlayniteAPI playniteAPI, RawgLibrarySettings settings)
        {
            var playniteStatuses = playniteAPI.Database.CompletionStatuses;

            foreach (var playniteStatus in playniteStatuses)
            {
                string rawgStatusId = null;

                if (settings?.PlayniteToRawgStatuses != null)
                    settings.PlayniteToRawgStatuses.TryGetValue(playniteStatus.Id, out rawgStatusId);

                if (rawgStatusId == null)
                    PlayniteToRawgStatusDefaults.TryGetValue(playniteStatus.Name, out rawgStatusId);

                yield return new PlayniteToRawgStatus(playniteStatus, rawgStatusId ?? "owned");
            }
        }

        public static IEnumerable<PlayniteToRawgRating> GetPlayniteToRawgRatings(RawgLibrarySettings settings)
        {
            foreach (var rating in RawgRatings)
            {
                Range range = null;

                if (settings?.PlayniteToRawgRatings != null)
                    settings.PlayniteToRawgRatings.TryGetValue(rating.Key, out range);

                if (range == null)
                {
                    if (rating.Key == 1)
                        range = new Range { Min = 0, Max = 40 };
                    else
                        range = new Range { Min = (rating.Key - 1) * 20 + 1, Max = rating.Key * 20 };
                }

                yield return new PlayniteToRawgRating(rating.Key, rating.Value, range);
            }
        }
    }
}
