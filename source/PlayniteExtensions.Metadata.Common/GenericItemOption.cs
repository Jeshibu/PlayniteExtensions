using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlayniteExtensions.Metadata.Common
{
    public class GenericItemOption<T> : GenericItemOption
    {
        public GenericItemOption(T item)
        {
            Item = item;
        }

        public T Item { get; }
    }
}
