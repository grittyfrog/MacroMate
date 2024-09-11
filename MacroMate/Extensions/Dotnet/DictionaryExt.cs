using System;
using System.Collections.Generic;

namespace MacroMate.Extensions.Dotnet;

public static class DictionaryExt {
    /**
     * <summary>Retrieve the element indexed by key, or create and add it and return the created value</summary>
     */
    public static TValue GetOrAdd<TKey, TValue>(
        this IDictionary<TKey, TValue> self,
        TKey key,
        Func<TValue> createFn
    ) {
        if (self.TryGetValue(key, out var value)) { return value; }

        var v = createFn();
        self.Add(key, v);
        return v;
    }

    public static void AddRange<TKey, TValue>(
        this IDictionary<TKey, TValue> self,
        IEnumerable<KeyValuePair<TKey, TValue>> values
    ) {
        foreach (var value in values) {
            self[value.Key] = value.Value;
        }
    }
}
