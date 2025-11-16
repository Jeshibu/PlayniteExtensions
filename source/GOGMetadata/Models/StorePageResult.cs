using System;
using System.Collections.Generic;

namespace GOGMetadata.Models;

public class StorePageResult
{
    public class ProductDetails
    {
        public class Feature
        {
            public string name;
            public string id;
        }

        public List<SluggedName> genres;
        public List<SluggedName> tags;
        public List<SluggedName> gameTags;
        public List<Feature> features;
        public string publisher;
        public List<SluggedName> developers;
        public DateTime? globalReleaseDate;
        public string id;
        public string galaxyBackgroundImage;
        public string backgroundImage;
        public string boxArtImage;
        public string image;
        public int size;
    }

    public ProductDetails cardProduct;
}