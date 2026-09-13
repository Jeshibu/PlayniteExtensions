using System.Collections.Generic;

namespace itchioBundleTagger;

public class Catalog
{
    public Dictionary<string, ItchIoGame> Games { get; set; }
}

public class ItchIoGame
{
    public string Id;
    public string Title;
    public string Steam;
    public string CurrentPrice;
    public Dictionary<string, string> Bundles;
}
