using System;
using System.Collections.Generic;
using System.Text;

namespace PlayniteExtensions.Metadata.Common
{
    public class PropertyImportSetting
    {
        public string Prefix { get; set; }
        public PropertyImportTarget ImportTarget { get; set; }
    }

    public enum PropertyImportTarget
    {
        Ignore,
        Genres,
        Tags,
        Series,
        Features,
    }

    [Flags]
    public enum DataSource
    {
        None = 0,
        Api = 1,
        Scraping = 2,
        ApiAndScraping = 3,
    }
}
