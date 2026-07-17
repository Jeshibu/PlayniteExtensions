using PlayniteExtensions.Metadata.Common;
using System;

namespace PCGamingWikiBulkImport.DataCollection;

public abstract class CargoFieldBase
{
    public string Table { get; set; }
    public string Field { get; set; }
    public string TableDisplayName => ToDisplayName(Table);
    public string FieldDisplayName => ToDisplayName(Field);

    public string TableAndFieldDisplayName => Table == CargoTables.Names.GameInfoBox
                                            ? FieldDisplayName
                                            : $"{ToDisplayName(Table)}: {FieldDisplayName}";

    private static string ToDisplayName(string name) => name.Replace('_', ' ');
}

public class CargoFieldInfo : CargoFieldBase
{
    public PropertyImportTarget PreferredField { get; set; } = PropertyImportTarget.Features;
    public CargoFieldType FieldType { get; set; }
    public string PageNamePrefix { get; set; }
    public Func<string, CargoValueWorkaround> ValueWorkaround { get; set; } = NormalValue;

    private static CargoValueWorkaround NormalValue(string str) => new() { Value = str };
}

public class CargoValueWorkaround
{
    public string Value { get; set; }
    public bool UseLike { get; set; } = false;
}

public enum CargoFieldType
{
    String,
    ListOfString,
}
