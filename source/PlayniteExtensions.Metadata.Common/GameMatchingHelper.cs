using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using PlayniteExtensions.Common;

namespace PlayniteExtensions.Metadata.Common
{
    public class GameMatchingHelper
    {
        public ConcurrentDictionary<string, string> DeflatedNames { get; } = new ConcurrentDictionary<string, string>();
        private SortableNameConverter sortableNameConverter = new SortableNameConverter(new string[0], numberLength: 1, removeEditions: true);

        public HashSet<string> GetDeflatedNames(IEnumerable<string> names)
        {
            return new HashSet<string>(names.Select(GetDeflatedName), StringComparer.InvariantCultureIgnoreCase);
        }

        public string GetDeflatedName(string name)
        {
            return DeflatedNames.GetOrAdd(name, x => sortableNameConverter.Convert(x).Deflate());
        }
    }
}
