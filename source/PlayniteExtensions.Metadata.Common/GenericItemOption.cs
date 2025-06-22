using Playnite.SDK;

namespace PlayniteExtensions.Metadata.Common;

public class GenericItemOption<T> : GenericItemOption
{
    public GenericItemOption(T item)
    {
        Item = item;
    }

    public T Item { get; }
}
