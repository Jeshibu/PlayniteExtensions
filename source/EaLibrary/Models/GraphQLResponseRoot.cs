using System.Collections.Generic;

namespace EaLibrary.Models;

public class GraphQlResponseRoot<TData>
{
    public TData data { get; set; }
    public List<GraphQlError> errors { get; set; }
}

public class GraphQlError
{
    public string message { get; set; }
    public GraphQlErrorLocation[] locations { get; set; }
    public string[] path { get; set; }
    public ErrorExtensions extensions { get; set; }
}

public class GraphQlErrorLocation
{
    public int line { get; set; }
    public int column { get; set; }
}

public class ErrorExtensions
{
    public string code { get; set; }
}
