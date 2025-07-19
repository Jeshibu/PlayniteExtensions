namespace GamesSizeCalculator.Models;

public class DepotInfo(string id, string name, ulong fileSize, bool isDlc, bool optional)
{
    public string Id { get; set; } = id;
    public string Name { get; set; } = name;
    public ulong FileSize { get; set; } = fileSize;
    public bool IsDLC { get; } = isDlc;
    public bool Optional { get; } = optional;
}