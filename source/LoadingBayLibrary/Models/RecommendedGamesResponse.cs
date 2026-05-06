namespace LoadingBayLibrary.Models;

public class RecommendedResponseRoot
{
    public RecommendedResponseData[] data { get; set; }
    public int code { get; set; }
    public string msg { get; set; }
}

public class RecommendedResponseData
{
    public RecommendedResponseAppData app_data { get; set; }
    /*
    public string main_image { get; set; }
    public string title { get; set; }
    public string description { get; set; }
    public int recommend_data_type { get; set; }
    public int status { get; set; }
    public int weight { get; set; }
    public string recommend_id { get; set; }
    public LoadingBayTag[] tags { get; set; }
    public bool available { get; set; }
    public object[] delete_ids { get; set; }
    */
}

public class RecommendedResponseAppData
{
    public int app_id { get; set; }
    public string display_name { get; set; }
    /*
    public string pegi_age_level { get; set; }
    public string goods_image { get; set; }
    public string goods_unique_name { get; set; }
    public int is_free { get; set; }
    public object free_trial { get; set; }
    public object early_access { get; set; }
    public bool kids_protection_open { get; set; }
    public string[] kids_protection_required_authorized_actions { get; set; }
    */
}

public class LoadingBayTag
{
    public int id { get; set; }
    public string display_name { get; set; }
}
