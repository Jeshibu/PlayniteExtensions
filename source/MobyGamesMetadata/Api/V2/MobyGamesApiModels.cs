﻿using System.Collections.Generic;
using System.Linq;

namespace MobyGamesMetadata.Api.V2;

public interface IHasPlatforms
{
    IEnumerable<string> Platforms { get; }
}

public class MobyGamesApiError
{
    public int Code { get; set; }
    public string Error { get; set; }
    public string Message { get; set; }
}

public class MobyGamesResult
{
    public List<MobyGame> games { get; set; }
}

public class MobyGame : IHasPlatforms
{
    public int game_id { get; set; }
    public string title { get; set; }
    public List<string> highlights { get; set; } = [];
    public string moby_url { get; set; }
    public List<MobyGenre> genres { get; set; } = [];
    public string description { get; set; }
    public string official_url { get; set; }
    public float? moby_score { get; set; }
    public List<MobyCoverGroup> covers { get; set; } = [];
    public List<MobyCompany> developers { get; set; } = [];
    public List<MobyCompany> publishers { get; set; } = [];
    public List<GamePlatform> platforms { get; set; } = [];
    public List<MobyScreenshotGroup> screenshots { get; set; } = [];
    public string release_date { get; set; }
    IEnumerable<string> IHasPlatforms.Platforms => platforms.Select(p => p.name);
}

public class MobyCompany : MobyIdName, IHasPlatforms
{
    public string url { get; set; }
    public List<string> platforms { get; set; } = [];

    IEnumerable<string> IHasPlatforms.Platforms => platforms;
}

public class MobyGenre : MobyIdName
{
    public string category { get; set; }
    public int category_id { get; set; }
}

public class MobyCoverGroup : IHasPlatforms
{
    public int cover_group_id { get; set; }
    public string comments { get; set; }
    public List<MobyCountry> countries { get; set; } = [];
    public List<MobyIdName> platforms { get; set; } = [];
    public List<MobyAttributeGroup> attributes { get; set; } = [];
    public List<MobyCover> images { get; set; } = [];

    IEnumerable<string> IHasPlatforms.Platforms => platforms.Select(p => p.name);
}

public class MobyAttributeGroup
{
    public string category { get; set; }
    public List<MobyIdName> attributes { get; set; } = [];
}

public class MobyScreenshotGroup: IHasPlatforms
{
    public int platform_id { get; set; }
    public string platform_name { get; set; }
    public List<MobyScreenshot> images { get; set; } = [];

    IEnumerable<string> IHasPlatforms.Platforms => [platform_name];
}

public class MobyCountry
{
    public int country_id { get; set; }
    public string name { get; set; }
    public string country_code { get; set; }
}

public abstract class MobyImage
{
    public string caption { get; set; }
    public int ordering { get; set; }
    public int thumbnail_width { get; set; }
    public int thumbnail_height { get; set; }
    public int width { get; set; }
    public int height { get; set; }
    public string moby_url { get; set; }
    public string thumbnail_url { get; set; }
    public string image_url { get; set; }
}

public class MobyCover : MobyImage
{
    public int cover_id { get; set; }
    public MobyIdName type { get; set; }
}

public class MobyIdName
{
    public int id { get; set; }
    public string name { get; set; }
}

public class MobyScreenshot : MobyImage
{
    public int screenshot_id { get; set; }
}

public class GamePlatform
{
    public int platform_id { get; set; }
    public string name { get; set; }
    public string release_date { get; set; }
}
