using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BigFishMetadata;

public class BigFishMetadataSettings : ObservableObject
{
    public BigFishLanguage SelectedLanguage { get; set; } = BigFishLanguage.English;
    public CommunityScoreType CommunityScoreType { get; set; } = CommunityScoreType.StarRating;

    [DontSerialize]
    public Dictionary<CommunityScoreType, string> CommunityScoreTypes => new()
    {
        { CommunityScoreType.StarRating, "Average star rating"},
        { CommunityScoreType.PercentageRecommended, "Percentage of recommendations" },
    };

    [DontSerialize]
    public List<BigFishLanguage> Languages => Enum.GetValues(typeof(BigFishLanguage))
        .Cast<BigFishLanguage>()
        .OrderBy(x => x.ToString())
        .ToList();
}

public enum BigFishLanguage
{
    Chinese = 132,
    Danish = 141,
    Dutch = 135,
    English = 114,
    French = 123,
    German = 117,
    Italian = 126,
    Japanese = 129,
    Portuguese = 144,
    Russian = 147,
    Spanish = 120,
    Swedish = 138,
}

public enum CommunityScoreType
{
    StarRating,
    PercentageRecommended,
}
