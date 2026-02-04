// ReSharper disable once CheckNamespace - So it will sit alongside the alternative rather than give a compile error
namespace System.Collections.Generic;
// ReSharper disable once InconsistentNaming - fine for extension types
public static class IReadOnlyDictionaryExtensions
{
    public static TValue? GetValueOrDefault<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue? defaultValue) where TValue : struct
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
    }
}