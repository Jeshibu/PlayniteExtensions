using Playnite.SDK;

namespace PlayniteExtensions.Metadata.Common;

public class GenericItemOption<T>(T item) : GenericItemOption
{
    public T Item { get; } = item;
}
