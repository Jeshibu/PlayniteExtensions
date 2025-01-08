using PlayniteExtensions.Metadata.Common;

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
        public EntityDefinitionInfo EntityDefinition { get; set; }

        public bool HasReferenceTable => EntityDefinition != null;
    }

    public class EntityDefinitionInfo : CargoFieldBase
    {
        public string Where { get; set; }
    }
}
