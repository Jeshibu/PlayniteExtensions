using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace LaunchBoxMetadata
{
    public class LaunchBoxXmlParser
    {
        private readonly string xmlPath;

        public LaunchBoxXmlParser(string xmlPath)
        {
            this.xmlPath = xmlPath;
        }

        public XmlData GetData()
        {
            var data = new XmlData();
            var xdoc = XDocument.Load(xmlPath);

            data.Games = xdoc.Root.Descendants("Game").Select(ParseGame);
            data.GameAlternateNames = xdoc.Root.Descendants("GameAlternateName").Select(ParseGameAlternateName);
            data.GameImages = xdoc.Root.Descendants("GameImage").Select(ParseGameImage);

            return data;
        }

        private static LaunchBoxGameName ParseGameAlternateName(XElement n)
        {
            try
            {
                var altName = new LaunchBoxGameName();
                altName.DatabaseID = n.Element("DatabaseID").Value;
                altName.Name = n.Element("AlternateName").Value;
                return altName;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static LaunchBoxGame ParseGame(XElement g)
        {
            try
            {
                var game = new LaunchBoxGame();
                game.Name = g.Element("Name").Value;
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
                game.DatabaseID = g.Element("DatabaseID").Value;
                if (double.TryParse(g.Element("CommunityRating")?.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out double communityRating))
                    game.CommunityRating = communityRating;

                game.Platform = g.Element("Platform").Value;
                game.ESRB = g.Element("ESRB")?.Value;
                game.CommunityRatingCount = int.Parse(g.Element("CommunityRatingCount").Value);
                game.Genres = g.Element("Genres")?.Value;
                game.Developer = g.Element("Developer")?.Value;
                game.Publisher = g.Element("Publisher")?.Value;
                return game;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private static LaunchBoxGameImage ParseGameImage(XElement i)
        {
            try
            {
                var img = new LaunchBoxGameImage();
                img.DatabaseID = i.Element("DatabaseID").Value;
                img.FileName = i.Element("FileName").Value;
                img.Type = i.Element("Type")?.Value;
                if (uint.TryParse(i.Element("CRC32").Value, out uint crc32))
                    img.CRC32 = crc32;
                return img;
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }

    public class XmlData
    {
        public IEnumerable<LaunchBoxGame> Games { get; set; }
        public IEnumerable<LaunchBoxGameName> GameAlternateNames { get; set; }
        public IEnumerable<LaunchBoxGameImage> GameImages { get; set; }
    }

    /*
  <GameImage>
    <DatabaseID>218762</DatabaseID>
    <FileName>58bb66c0-733a-428e-92c1-c101140a0505.png</FileName>
    <Type>Screenshot - Gameplay</Type>
    <CRC32>4250318803</CRC32>
  </GameImage>
     */
}
