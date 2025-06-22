using System.Collections.Generic;

namespace PlayniteExtensions.Metadata.Common;

public interface IImageData
{
    string Url { get; }
    string ThumbnailUrl { get; }
    int Width { get; }
    int Height { get; }
    IEnumerable<string> Platforms { get; }
}

public class BasicImage : IImageData
{
    public BasicImage(string url)
    {
        Url = url;
    }

    public string Url { get; set; }
    public string ThumbnailUrl { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public IEnumerable<string> Platforms { get; set; } = new List<string>();
}
