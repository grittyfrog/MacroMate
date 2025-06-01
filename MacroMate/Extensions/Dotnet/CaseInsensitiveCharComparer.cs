using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace MacroMate.Extensions.Dotnet;

public class CaseInsensitiveCharComparer : IEqualityComparer<char>, IComparer<char> {
    private readonly CompareInfo _compareInfo = CultureInfo.InvariantCulture.CompareInfo;
    private readonly CompareOptions _options = CompareOptions.IgnoreCase;

    public bool Equals(char x, char y) {
        return Compare(x, y) is 0;
    }

    public int Compare(char x, char y) {
        return _compareInfo.Compare(new ReadOnlySpan<char>(ref x), new(ref y), _options);
    }


    public int GetHashCode([DisallowNull] char ch) {
        return _compareInfo.GetHashCode(new ReadOnlySpan<char>(ref ch), _options);
    }
}
