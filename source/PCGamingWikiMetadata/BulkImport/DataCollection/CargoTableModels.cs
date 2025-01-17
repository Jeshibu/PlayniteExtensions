using PlayniteExtensions.Metadata.Common;
using System;

namespace PCGamingWikiBulkImport.DataCollection
{
    public abstract class CargoFieldBase
    {
        public string Table { get; set; }
        public string Field { get; set; }
        public string FieldDisplayName
        {
            get
            {
                var fieldDisplayName = ToDisplayName(Field);
                if (Table == CargoTables.GameInfoBoxTableName)
                    return fieldDisplayName;

                return $"{ToDisplayName(Table)}: {fieldDisplayName}";
            }
        }

        private static string ToDisplayName(string name) => name.Replace('_', ' ');
    }

    public class CargoFieldInfo : CargoFieldBase
    {
        public PropertyImportTarget PreferredField { get; set; } = PropertyImportTarget.Features;
        public CargoFieldType FieldType { get; set; }
        public string PageNamePrefix { get; set; }
        public Func<string,CargoValueWorkaround> ValueWorkaround { get; set; } = v => new CargoValueWorkaround { Value = v };
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
}
