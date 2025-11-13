using Newtonsoft.Json;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PlayniteExtensions.Common;

public static class IEnumerableExtensions
{
    private static ILogger Logger => field ??= LogManager.GetLogger();

    public static Dictionary<TKey, TIn> ToDictionarySafe<TIn, TKey>(this IEnumerable<TIn> input, Func<TIn, TKey> keySelector, IEqualityComparer<TKey> equalityComparer = null, bool favorBiggerObject = false)
    {
        return ToDictionarySafe(input, keySelector, item => item, equalityComparer, favorBiggerObject);
    }

    public static Dictionary<TKey, TValue> ToDictionarySafe<TIn, TKey, TValue>(this IEnumerable<TIn> input, Func<TIn, TKey> keySelector, Func<TIn, TValue> valueSelector, IEqualityComparer<TKey> equalityComparer = null, bool favorBiggerObject = false)
    {
        Dictionary<TKey, TValue> output;
        if (equalityComparer == null)
            output = [];
        else
            output = new(equalityComparer);

        foreach (var item in input)
        {
            var key = keySelector(item);
            var value = valueSelector(item);
            if (output.TryGetValue(key, out TValue existingValue))
            {
                var stackTrace = new StackTrace(true);
                var existingValueString = existingValue == null ? null : JsonConvert.SerializeObject(existingValue, new JsonSerializerSettings { MaxDepth = 5 });
                var newValueString = value == null ? null : JsonConvert.SerializeObject(value, new JsonSerializerSettings { MaxDepth = 5 });
                var stacktraceString = string.Join(string.Empty, stackTrace.GetFrames()!.Select(f => f.ToString()));
                var logString = $@"An item with the same key has already been added: {key}
existing value: {existingValueString}
new value: {newValueString}
stacktrace: {stacktraceString}";
                Logger.Warn(logString);
                if (favorBiggerObject && (newValueString?.Length ?? 0) > (existingValueString?.Length ?? 0))
                    output[key] = value;
                continue;
            }
            output.Add(key, value);
        }

        return output;
    }

    public static ICollection<T> NullIfEmpty<T>(this ICollection<T> items)
    {
        if (items is { Count: > 0 })
            return items;
        else
            return null;
    }
}
