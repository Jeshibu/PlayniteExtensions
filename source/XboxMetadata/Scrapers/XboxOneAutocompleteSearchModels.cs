using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XboxMetadata.Scrapers
{
    public class XboxSearchResultGame
    {
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string Id { get; set; }
        public string Url { get; set; }
        public string ProductType { get; set; }
    }

    public class XboxSearchResultsRoot
    {
        public string Query { get; set; }
        public XboxSearchResultSet[] ResultSets { get; set; }
    }

    public class XboxSearchResultSet
    {
        public string Source { get; set; }
        public bool FromCache { get; set; }
        public string Type { get; set; }
        public XboxSearchSuggest[] Suggests { get; set; }
    }

    public class XboxSearchSuggest
    {
        public string Source { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Url { get; set; }
        public string ImageUrl { get; set; }
        public bool Curated { get; set; }
        public XboxMetadataItem[] Metas { get; set; }
    }

    public class XboxMetadataItem
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
