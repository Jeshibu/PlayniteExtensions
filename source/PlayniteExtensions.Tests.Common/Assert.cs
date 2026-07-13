using System.Collections.Generic;
using System.Linq;

namespace Xunit;

public static class AssertHelper
{
    public static void CollectionsHaveSameItems<T>(ICollection<T> expected, ICollection<T> actual, IEqualityComparer<T> comparer = null)
    {
        Assert.NotNull(actual);
        comparer ??= EqualityComparer<T>.Default;

        foreach (var expectedItem in expected)
            Assert.Contains(expectedItem, actual, comparer);

        var extraItems = actual.Except(expected, comparer).ToList();

        if (extraItems.Any())
            throw new($"Unexpected extra items: {string.Join(", ", extraItems)}");

        if (expected.Count != actual.Count)
            throw new($"Collection has {actual.Count} items, but expected {expected.Count}.");
    }
}
