using Playnite.SDK;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace GamersGateLibrary;

public static class EnumExtensions
{
    extension(Enum source)
    {
        public int GetMax() => Enum.GetValues(source.GetType()).Cast<int>().Max();

        public int GetMin() => Enum.GetValues(source.GetType()).Cast<int>().Min();

        public string GetDescription()
        {
            FieldInfo field = source.GetType().GetField(source.ToString());
            if (field == null)
            {
                return string.Empty;
            }

            var attributes = (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);
            if (attributes.Length > 0)
            {
                var desc = attributes[0].Description;
                if (desc.StartsWith("LOC"))
                {
                    return ResourceProvider.GetString(desc);
                }
                else
                {
                    return attributes[0].Description;
                }
            }
            else
            {
                return source.ToString();
            }
        }
    }
}
