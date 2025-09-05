using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace LaunchBoxMetadata;

public class LaunchBoxXmlParser(string xmlPath)
{
    private readonly ILogger logger = LogManager.GetLogger();

    public XmlData GetData()
    {
        var data = new XmlData();
        var xdoc = XDocument.Load(xmlPath);

        data.Games = xdoc.Root.Descendants("Game")
            .Select(ParseGame).Where(FilterObject).ToList();

        data.GameAlternateNames = xdoc.Root.Descendants("GameAlternateName")
            .Select(ParseGameAlternateName).Where(FilterObject).ToList();

        data.GameImages = xdoc.Root.Descendants("GameImage")
            .Select(ParseGameImage).Where(FilterObject).ToList();

        logger.Info($"Parsed {data.Games.Count} games, {data.GameAlternateNames.Count} alternate game names, {data.GameImages.Count} images");

        return data;
    }

    private bool FilterObject(IDatabaseObject obj) => obj.DatabaseID != default;

    private LaunchBoxGameName ParseGameAlternateName(XElement n) => new()
    {
        DatabaseID = GetDatabaseId(n),
        Name = n.Element("AlternateName")?.Value
    };

    private LaunchBoxGame ParseGame(XElement g)
    {
        var game = new LaunchBoxGame
        {
            DatabaseID = GetDatabaseId(g),
            Name = g.Element("Name")?.Value,
        };

        if (DateTime.TryParse(g.Element("ReleaseDate")?.Value, out DateTime releaseDate))
            game.ReleaseDate = releaseDate;

        if (int.TryParse(g.Element("ReleaseYear")?.Value, out int releaseYear))
            game.ReleaseYear = releaseYear;

        game.Overview = g.Element("Overview")?.Value;
        if (int.TryParse(g.Element("MaxPlayers")?.Value, out int maxPlayers))
            game.MaxPlayers = maxPlayers;

        game.ReleaseType = g.Element("ReleaseType")?.Value;
        if (bool.TryParse(g.Element("Cooperative")?.Value, out bool cooperative))
            game.Cooperative = cooperative;

        game.WikipediaURL = g.Element("WikipediaURL")?.Value;
        game.VideoURL = g.Element("VideoURL")?.Value;

        if (double.TryParse(g.Element("CommunityRating")?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out double communityRating))
            game.CommunityRating = communityRating;

        game.Platform = g.Element("Platform")?.Value;
        game.ESRB = g.Element("ESRB")?.Value;
        game.CommunityRatingCount = int.Parse(g.Element("CommunityRatingCount").Value);
        game.Genres = g.Element("Genres")?.Value;
        game.Developer = g.Element("Developer")?.Value;
        game.Publisher = g.Element("Publisher")?.Value;
        return game;
    }

    private LaunchBoxGameImage ParseGameImage(XElement i)
    {
        var img = new LaunchBoxGameImage();
        img.DatabaseID = GetDatabaseId(i);
        img.FileName = i.Element("FileName")?.Value;
        img.Type = i.Element("Type")?.Value;
        img.Region = i.Element("Region")?.Value;
        if (uint.TryParse(i.Element("CRC32")?.Value, out uint crc32))
            img.CRC32 = crc32;
        return img;
    }

    private long GetDatabaseId(XElement i)
    {
        if (long.TryParse(i.Element("DatabaseID")?.Value, out long dbId))
            return dbId;
        
        logger.Warn($"No Database ID found in XML element {i}");
        return default;
    }
}

public class XmlData
{
    public ICollection<LaunchBoxGame> Games { get; set; }
    public ICollection<LaunchBoxGameName> GameAlternateNames { get; set; }
    public ICollection<LaunchBoxGameImage> GameImages { get; set; }
}