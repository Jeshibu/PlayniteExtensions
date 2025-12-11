namespace BigFishMetadata.Models;

public class ProductData
{
    public Products products { get; set; }
}

public class Products
{
    public Items[] items { get; set; }
    public string __typename { get; set; }
}

public class Items
{
    public int id { get; set; }
    public string uid { get; set; }
    public string __typename { get; set; }
    public Categories[] categories { get; set; }
    public Description description { get; set; }
    public Short_description short_description { get; set; }
    public Media_gallery_entries[] media_gallery_entries { get; set; }
    public string meta_description { get; set; }
    public string name { get; set; }
    public Price price { get; set; }
    public Price_range price_range { get; set; }
    public string sku { get; set; }
    public Small_image small_image { get; set; }
    public string stock_status { get; set; }
    public string url_key { get; set; }
    public Custom_attributes[] custom_attributes { get; set; }
}

public class Categories
{
    public string uid { get; set; }
    public string name { get; set; }
    public string url_path { get; set; }
    public string url_key { get; set; }
    public int include_in_menu { get; set; }
    public Breadcrumbs[] breadcrumbs { get; set; }
    public string __typename { get; set; }
}

public class Breadcrumbs
{
    public string category_uid { get; set; }
    public string __typename { get; set; }
}

public class Description
{
    public string html { get; set; }
    public string __typename { get; set; }
}

public class Short_description
{
    public string html { get; set; }
    public string __typename { get; set; }
}

public class Media_gallery_entries
{
    public string uid { get; set; }
    public string label { get; set; }
    public int position { get; set; }
    public bool disabled { get; set; }
    public string file { get; set; }
    public string __typename { get; set; }
}

public class Custom_attributes
{
    public Selected_attribute_options selected_attribute_options { get; set; }
    public Entered_attribute_value entered_attribute_value { get; set; }
    public Attribute_metadata attribute_metadata { get; set; }
    public string __typename { get; set; }
}

public class Selected_attribute_options
{
    public Attribute_option[] attribute_option { get; set; }
    public string __typename { get; set; }
}

public class Attribute_option
{
    public string uid { get; set; }
    public string label { get; set; }
    public bool is_default { get; set; }
    public string __typename { get; set; }
}

public class Entered_attribute_value
{
    public string value { get; set; }
    public string __typename { get; set; }
}

public class Attribute_metadata
{
    public string uid { get; set; }
    public string code { get; set; }
    public string label { get; set; }
    public Attribute_labels[] attribute_labels { get; set; }
    public string data_type { get; set; }
    public bool is_system { get; set; }
    public string entity_type { get; set; }
    public Ui_input ui_input { get; set; }
    public string[] used_in_components { get; set; }
    public string __typename { get; set; }
}

public class Attribute_labels
{
    public string store_code { get; set; }
    public string label { get; set; }
    public string __typename { get; set; }
}

public class Ui_input
{
    public string ui_input_type { get; set; }
    public bool is_html_allowed { get; set; }
    public string __typename { get; set; }
}
