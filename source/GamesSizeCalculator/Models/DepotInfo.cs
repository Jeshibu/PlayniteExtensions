namespace GamesSizeCalculator.Models;

public class DepotInfo(string id, string name, ulong fileSize, bool isDlc, bool optional)
{
    public string Id { get; } = id;
    public string Name { get; } = name;
    public ulong FileSize { get; } = fileSize;
    public bool IsDLC { get; } = isDlc;
    public bool Optional { get; } = optional;
}