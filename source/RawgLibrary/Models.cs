using Playnite.SDK;
using Playnite.SDK.Data;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RawgLibrary;

public class Range
{
    private int min;
    private int max;

    public int Min { get => min; set => min = value; }
    public int Max { get => max; set => max = value; }

    [DontSerialize]
    public string MinString { get => Min.ToString(); set => int.TryParse(value, out min); }

    [DontSerialize]
    public string MaxString { get => Max.ToString(); set => int.TryParse(value, out max); }
}

public class RawgToPlayniteStatus(string id, string description, Guid playniteCompletionStatusId)
{
    public string Id { get; set; } = id;
    public string Description { get; set; } = description;
    public Guid PlayniteCompletionStatusId { get; set; } = playniteCompletionStatusId;
}

public class RawgToPlayniteRating
{
    private int playniteRating;

    public int Id { get; set; }
    public string Description { get; set; }
    public int PlayniteRating { get => playniteRating; set => playniteRating = value; }
    public string PlayniteRatingString
    {
        get => PlayniteRating.ToString();
        set
        {
            if (!int.TryParse(value, out playniteRating))
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

public class PlayniteToRawgStatus(CompletionStatus playniteCompletionStatus, string rawgStatusId)
{
    public CompletionStatus PlayniteCompletionStatus { get; set; } = playniteCompletionStatus;
    public string RawgStatusId { get; set; } = rawgStatusId;
}

public class PlayniteToRawgRating(int id, string description, Range range)
{
    public int Id { get; set; } = id;
    public string Description { get; set; } = description;
    public Range Range { get; set; } = range;
}

public static class RawgMapping
{
    private static ILogger logger = LogManager.GetLogger();
    public static Guid DoNotImportId = new("6e18323f-8798-455c-851c-79bf34d83466");

    //"Not Played", "Played", "Beaten", "Completed", "Playing", "Abandoned", "On Hold", "Plan to Play"
    public static Dictionary<string, string> RawgCompletionStatuses = new()
    {
        { "owned", "Uncategorized" },
        { "playing", "Currently Playing" },
        { "beaten", "Completed" },
        { "dropped", "Played" },
        { "yet", "Not played" },
        { "toplay", "Wishlist" },
    };

    private static Dictionary<string, string> RawgToPlayniteStatusDefaults = new()
    {
        { "owned", "Not Played" },
        { "playing", "Playing" },
        { "beaten", "Beaten" },
        { "dropped", "Abandoned" },
        { "yet", "Not Played" },
        { "toplay", "Wishlist" }, //there's no default completion status for this, just try and see if Wishlist exists
    };

    public static Dictionary<int, string> RawgRatings = new()
    {
        { 1, "skip" },
        { 3, "meh" },
        { 4, "recommended" },
        { 5, "excellent" },
    };

    private static Dictionary<string, string> PlayniteToRawgStatusDefaults = new()
    {
        { "Not Played", "yet" },
        { "Played", "owned" },
        { "Beaten", "beaten" },
        { "Completed", "beaten" },
        { "Playing", "playing" },
        { "Abandoned", "dropped" },
        { "On Hold", "owned" },
        { "Plan to Play", "yet" },
        { "Wislist", "toplay" },
        { "Wishlisted", "toplay" },
        { "None", "owned" },
    };

    public static IEnumerable<RawgToPlayniteStatus> GetRawgToPlayniteCompletionStatuses(IPlayniteAPI playniteAPI, RawgLibrarySettings settings)
    {
        var playniteStatuses = playniteAPI.Database.CompletionStatuses;

        foreach (var cs in RawgCompletionStatuses)
        {
            CompletionStatus playniteStatus = null;

            if (settings?.RawgToPlayniteStatuses != null && settings.RawgToPlayniteStatuses.TryGetValue(cs.Key, out Guid? id) && id.HasValue)
            {
                if (id == Guid.Empty)
                    playniteStatus = new CompletionStatus("Default") { Id = Guid.Empty };
                else
                    playniteStatus = playniteStatuses[id.Value];
            }

            if (playniteStatus == null && RawgToPlayniteStatusDefaults.TryGetValue(cs.Key, out string playniteStatusName))
                playniteStatus = playniteStatuses.FirstOrDefault(s => s.Name.Equals(playniteStatusName, StringComparison.InvariantCultureIgnoreCase));

            playniteStatus ??= playniteStatuses[playniteAPI.ApplicationSettings.CompletionStatus.DefaultStatus];

            playniteStatus ??= playniteStatuses.FirstOrDefault();

            logger.Trace($"Completion statuses: {playniteStatuses?.Count}, selected completion status: {playniteStatus?.Name}");

            yield return new RawgToPlayniteStatus(cs.Key, cs.Value, playniteStatus.Id);
        }
    }

    public static IEnumerable<RawgToPlayniteRating> GetRawgToPlayniteRatings(RawgLibrarySettings settings)
    {
        foreach (var r in RawgRatings)
        {
            if (settings?.RawgToPlayniteRatings == null || !settings.RawgToPlayniteRatings.TryGetValue(r.Key, out int rating))
                rating = r.Key * 20;

            yield return new RawgToPlayniteRating(r.Key, r.Value, rating);
        }
    }

    public static IEnumerable<PlayniteToRawgStatus> GetPlayniteToRawgStatuses(IPlayniteAPI playniteAPI, RawgLibrarySettings settings)
    {
        var playniteStatuses = playniteAPI.Database.CompletionStatuses.ToList();
        playniteStatuses.Add(new CompletionStatus { Id = Guid.Empty, Name = "None" });

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
