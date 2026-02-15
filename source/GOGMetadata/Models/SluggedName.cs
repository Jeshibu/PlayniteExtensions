namespace GOGMetadata.Models;

public class SluggedName
{
    public string Name { get; set; }
    public string Slug { get; set; }

    public SluggedName()
    {
    }

    public SluggedName(string slug, string name)
    {
        Slug = slug;
        Name = name;
    }
}
