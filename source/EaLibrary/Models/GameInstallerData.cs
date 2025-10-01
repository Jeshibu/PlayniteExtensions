using System.Collections.Generic;
using System.Xml.Serialization;

namespace EaLibrary.Models;

[XmlRoot("DiPManifest")]
public class GameInstallerData
{
    [XmlElement("runtime")]
    public Runtime runtime { get; set; }
    
    public GameTitles GameTitles { get; set; }
    
    [XmlIgnore]
    public string Location { get; set; }
    
    [XmlIgnore]
    public string InstallDirectory { get; set; }
}

[XmlType]
public class Runtime
{
    [XmlElement("launcher")]
    public List<Launcher> launchers { get; set; }
}

public class Launcher
{
    public string filePath { get; set; }
    public string parameters { get; set; }
    public bool executeElevated { get; set; }
    public bool requires64BitOS { get; set; }
    public bool trial { get; set; }
}

public class GameTitles
{
    [XmlArray("gameTitle")]
    public List<GameTitle> Titles { get; set; } = [];
}

public class GameTitle
{
    [XmlAttribute("locale")]
    public string Locale { get; set; }
    
    [XmlElement]
    public string Name { get; set; }
}