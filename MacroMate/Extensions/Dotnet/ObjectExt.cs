using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MacroMate.Extensions.Dotnet;

public static class ObjectExt {
    public static R Let<T, R>(this T self, Func<T, R> block) {
        return block(self);
    }

    public static T Also<T>(this T self, Action<T> block) {
        block(self);
        return self;
    }

    /**
     * Return null if predicate returns true, otherwise return self
     */
    public static T? DefaultIf<T>(this T self, Func<T, Boolean> predicate) {
        return predicate(self) ? default(T) : self;
    }
}
