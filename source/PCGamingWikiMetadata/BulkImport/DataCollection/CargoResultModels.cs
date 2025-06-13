using System.Collections.Generic;

namespace PCGamingWikiBulkImport.DataCollection;

public class CargoResultRoot<T>
{
    public ResultLimits Limits { get; set; }
    public List<CargoTitle<T>> CargoQuery { get; set; } = new List<CargoTitle<T>>();
}

public class ResultLimits
{
    public int CargoQuery { get; set; }
}

public class CargoTitle<T>
{
    public T Title { get; set; }
}

public class ItemCount
{
    public string Value { get; set; }
    public int Count { get; set; }
}

public class CargoResultGame
{
    public string Name { get; set; }
    public string Released { get; set; }
    public string OS { get; set; }
    public string SteamID { get; set; }
    public string GOGID { get; set; }
    public string Value { get; set; }
}
