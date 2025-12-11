using BigFishMetadata.Models;
using Newtonsoft.Json;
using PlayniteExtensions.Common;
using System.Web;

namespace BigFishMetadata;

public class BigFishGraphQLService(IWebDownloader downloader)
{
    public static string GetSearchUrl(string query) =>
        $"https://www.bigfishgames.com/graphql?query=query+ProductSearch%28%24currentPage%3AInt%3D1%24inputText%3AString%21%24pageSize%3AInt%3D6%24filters%3AProductAttributeFilterInput%21%24sort%3AProductAttributeSortInput%29%7Bproducts%28currentPage%3A%24currentPage+pageSize%3A%24pageSize+search%3A%24inputText+filter%3A%24filters+sort%3A%24sort%29%7Bitems%7Bid+uid+name+price%7BregularPrice%7Bamount%7Bcurrency+value+__typename%7D__typename%7D__typename%7Dprice_range%7Bmaximum_price%7Bfinal_price%7Bcurrency+value+__typename%7Dregular_price%7Bcurrency+value+__typename%7Ddiscount%7Bamount_off+__typename%7D__typename%7D__typename%7Dproduct_list_date+sku+small_image%7Burl+__typename%7Dimage_feature_url+platform+language+product_delist_date+stock_status+rating_summary+__typename+url_key%7Dpage_info%7Btotal_pages+__typename%7Dtotal_count+__typename%7D%7D&operationName=ProductSearch&variables=%7B%22currentPage%22%3A1%2C%22pageSize%22%3A12%2C%22filters%22%3A%7B%7D%2C%22inputText%22%3A%22{HttpUtility.UrlEncode(query)}%22%2C%22sort%22%3A%7B%22relevance%22%3A%22DESC%22%7D%7D";

    public static string GetDetailsUrl(string urlKey) =>
        $"https://www.bigfishgames.com/graphql?query=query+getProductDetailForProductPage%28%24urlKey%3AString%21%29%7Bproducts%28filter%3A%7Burl_key%3A%7Beq%3A%24urlKey%7D%7D%29%7Bitems%7Bid+uid+...ProductDetailsFragment+__typename%7D__typename%7D%7Dfragment+ProductDetailsFragment+on+ProductInterface%7B__typename+categories%7Buid+name+url_path+url_key+include_in_menu+breadcrumbs%7Bcategory_uid+__typename%7D__typename%7Ddescription%7Bhtml+__typename%7Dshort_description%7Bhtml+__typename%7Did+uid+media_gallery_entries%7Buid+label+position+disabled+file+__typename%7Dmeta_description+name+price%7BregularPrice%7Bamount%7Bcurrency+value+__typename%7D__typename%7D__typename%7Dprice_range%7Bmaximum_price%7Bfinal_price%7Bcurrency+value+__typename%7Dregular_price%7Bcurrency+value+__typename%7Ddiscount%7Bamount_off+percent_off+__typename%7D__typename%7D__typename%7Dsku+small_image%7Burl+__typename%7Dstock_status+url_key+custom_attributes%7Bselected_attribute_options%7Battribute_option%7Buid+label+is_default+__typename%7D__typename%7Dentered_attribute_value%7Bvalue+__typename%7Dattribute_metadata%7Buid+code+label+attribute_labels%7Bstore_code+label+__typename%7Ddata_type+is_system+entity_type+ui_input%7Bui_input_type+is_html_allowed+__typename%7D...on+ProductAttributeMetadata%7Bused_in_components+__typename%7D__typename%7D__typename%7D...on+ConfigurableProduct%7Bconfigurable_options%7Battribute_code+attribute_id+uid+label+values%7Buid+default_label+label+store_label+use_default_value+value_index+swatch_data%7B...on+ImageSwatchData%7Bthumbnail+__typename%7Dvalue+__typename%7D__typename%7D__typename%7Dvariants%7Battributes%7Bcode+value_index+__typename%7Dproduct%7Buid+media_gallery_entries%7Buid+disabled+file+label+position+__typename%7Dsku+stock_status+price%7BregularPrice%7Bamount%7Bcurrency+value+__typename%7D__typename%7D__typename%7Dprice_range%7Bmaximum_price%7Bfinal_price%7Bcurrency+value+__typename%7Dregular_price%7Bcurrency+value+__typename%7Ddiscount%7Bamount_off+percent_off+__typename%7D__typename%7D__typename%7Dcustom_attributes%7Bselected_attribute_options%7Battribute_option%7Buid+label+is_default+__typename%7D__typename%7Dentered_attribute_value%7Bvalue+__typename%7Dattribute_metadata%7Buid+code+label+attribute_labels%7Bstore_code+label+__typename%7Ddata_type+is_system+entity_type+ui_input%7Bui_input_type+is_html_allowed+__typename%7D...on+ProductAttributeMetadata%7Bused_in_components+__typename%7D__typename%7D__typename%7D__typename%7D__typename%7D__typename%7D%7D&operationName=getProductDetailForProductPage&variables=%7B%22urlKey%22%3A%22{urlKey}%22%7D";

    public SearchResultData Search(string query) => Get<SearchResultData>(GetSearchUrl(query));

    public ProductData GetProductDetails(string urlKey) => Get<ProductData>(GetDetailsUrl(urlKey));

    private T Get<T>(string url)
    {
        var response = downloader.DownloadString(url, throwExceptionOnErrorResponse: true);
        var obj = JsonConvert.DeserializeObject<ResponseRoot<T>>(response.ResponseContent);
        return obj.data;
    }
}
