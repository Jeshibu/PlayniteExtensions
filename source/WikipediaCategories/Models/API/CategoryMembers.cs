namespace WikipediaCategories.Models.API;

public class CategoryMemberQueryResult
{
    public CategoryMember[] categorymembers { get; set; }
}

public class CategoryMember
{
    public int ns { get; set; }
    public string title { get; set; }
    public string type { get; set; }
}

