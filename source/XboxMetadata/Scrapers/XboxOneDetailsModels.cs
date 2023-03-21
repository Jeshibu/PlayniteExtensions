using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XboxMetadata.Scrapers
{
    public class XboxGameDetailsRoot
    {
        public XboxGameDetailsCore2 Core2 { get; set; }
    }

    public class XboxGameDetailsCore2
    {
        public XboxGameDetailsProducts Products { get; set; }
    }

    public class XboxGameDetailsProducts
    {
        public Dictionary<string, XboxGameDetailsProductSummary> ProductSummaries { get; set; }
    }

    public class XboxGameDetailsProductSummary
    {
        public string ProductId { get; set; }
        public XboxGameDetailsAccessibilityCapabilities AccessibilityCapabilities { get; set; }
        public string[] AvailableOn { get; set; }
        public double? AverageRating { get; set; }
        public Dictionary<string, string> Capabilities { get; set; }
        public string[] Categories { get; set; }
        public XboxGameDetailsAgeRating ContentRating { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public string DeveloperName { get; set; }
        public string PublisherName { get; set; }
        public XboxGameDetailsImages Images { get; set; }
        public Dictionary<string, XboxGameLanguageSupport> LanguagesSupported { get; set; }
        public ulong MaxInstallSize { get; set; }
        public DateTime ReleaseDate { get; set; }
        public string Title { get; set; }
        //there's also a "videos" property that can't be used right now
    }

    public class XboxGameLanguageSupport
    {
        public string LanguageDisplayName { get; set; }
        public bool AreSubtitlesSupported { get; set; }
        public bool IsAudioSupported { get; set; }
        public bool IsInterfaceSupported { get; set; }
    }

    public class XboxGameDetailsImages
    {
        public XboxImageDetails BoxArt { get; set; }
        public XboxImageDetails Poster { get; set; }
        public XboxImageDetails SuperHeroArt { get; set; }
        public XboxImageDetails[] Screenshots { get; set; }
    }

    public class XboxImageDetails
    {
        public string Url { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public string GetResizedUrl(int maxWidth, int maxHeight, int quality = 90)
        {
            int w = Math.Min(Width, maxWidth);
            int h = Math.Min(Height, maxHeight);
            return $"{Url}?q={quality}&w={w}&h={h}";
        }
    }

    public class XboxGameDetailsAccessibilityCapabilities
    {
        public string[] Audio { get; set; }
        public string[] Gameplay { get; set; }
        public string[] Input { get; set; }
        public string[] Visual { get; set; }
        public string PublisherInformationUri { get; set; }
    }

    public class XboxGameDetailsAgeRating
    {
        public string BoardName { get; set; }
        public string Description { get; set; }
        public object[] Disclaimers { get; set; }
        public string[] Descriptors { get; set; }
        public string ImageUri { get; set; }
        public string ImageLinkUri { get; set; }
        public string[] InteractiveDescriptions { get; set; }
        public string Rating { get; set; }
        public int RatingAge { get; set; }
        public string RatingDescription { get; set; }
    }
}
