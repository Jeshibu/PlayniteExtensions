using Playnite.SDK.Models;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlayniteExtensions.Metadata.Common
{
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

    public interface IHasName
    {
        string Name { get; }
    }
}
