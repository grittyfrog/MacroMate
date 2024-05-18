using System;
using System.Linq;

namespace MacroMate.Extensions.Dotnet;

public static class StringExt {
    public static string? NullIfEmpty(this string self) => self != "" ? self : null;

    public static string IfEmpty(this string self, string replacement) => self != "" ? self : replacement;

    public static string Truncate(this string self, int maxLength) {
        if (self.Length > maxLength) {
            return self.Substring(0, maxLength);
        }

        return self;
    }

    public static uint? ToUIntOrNull(this string self) {
        uint outValue;
        return uint.TryParse(self, out outValue) ? outValue : null;
    }

    public static DateTime? ToDateTimeOrNull(this string self) {
        DateTime outValue;
        return DateTime.TryParse(self, out outValue) ? outValue : null;
    }

    public static int MaxLineLength(this string self) {
        return Enumerable.Max(self.Split("\n").Select(s => s.Count()));
    }
}
