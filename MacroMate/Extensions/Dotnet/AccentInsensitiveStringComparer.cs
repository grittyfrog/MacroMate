using System;
using System.Collections.Generic;
using System.Globalization;

namespace MacroMate.Extensions.Dotnet;

public class AccentInsensitiveStringComparer : IEqualityComparer<string> {
    private readonly CompareInfo _compareInfo = CultureInfo.InvariantCulture.CompareInfo;
    private readonly CompareOptions _options = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

    public bool Equals(string? x, string? y) {
        if (x == null && y == null) return true;
        if (x == null || y == null) return false;
        return _compareInfo.Compare(x, y, _options) == 0;
    }

    public int GetHashCode(string obj) {
        if (obj == null) return 0;
        return _compareInfo.GetHashCode(obj, _options);
    }
}
