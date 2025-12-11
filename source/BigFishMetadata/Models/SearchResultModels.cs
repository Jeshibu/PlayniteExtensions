namespace BigFishMetadata.Models;

public class SearchResultData
{
    public SearchResultProducts products { get; set; }
}

public class SearchResultProducts
{
    public SearchResultItem[] items { get; set; }
    public Page_info page_info { get; set; }
    public int total_count { get; set; }
    public string __typename { get; set; }
}

public class SearchResultItem
{
    public int id { get; set; }
    public string uid { get; set; }
    public string name { get; set; }
    public Price price { get; set; }
    public Price_range price_range { get; set; }
    public string product_list_date { get; set; }
    public string sku { get; set; }
    public Small_image small_image { get; set; }
    public string image_feature_url { get; set; }
    public int platform { get; set; }
    public int language { get; set; }
    public string product_delist_date { get; set; }
    public string stock_status { get; set; }
    public int rating_summary { get; set; }
    public string __typename { get; set; }
    public string url_key { get; set; }
}

public class Price
{
    public RegularPrice regularPrice { get; set; }
    public string __typename { get; set; }
}

public class RegularPrice
{
    public Amount amount { get; set; }
    public string __typename { get; set; }
}

public class Amount
{
    public string currency { get; set; }
    public double value { get; set; }
    public string __typename { get; set; }
}

public class Price_range
{
    public Maximum_price maximum_price { get; set; }
    public string __typename { get; set; }
}

public class Maximum_price
{
    public Final_price final_price { get; set; }
    public Regular_price regular_price { get; set; }
    public Discount discount { get; set; }
    public string __typename { get; set; }
}

public class Final_price
{
    public string currency { get; set; }
    public double value { get; set; }
    public string __typename { get; set; }
}

public class Regular_price
{
    public string currency { get; set; }
    public double value { get; set; }
    public string __typename { get; set; }
}

public class Discount
{
    public int amount_off { get; set; }
    public string __typename { get; set; }
}

public class Small_image
{
    public string url { get; set; }
    public string __typename { get; set; }
}

public class Page_info
{
    public int total_pages { get; set; }
    public string __typename { get; set; }
}
