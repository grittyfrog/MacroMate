using System;
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

    /// <summary>
    /// Uses reflection to get the field value from an object.
    /// </summary>
    ///
    /// <param name="type">The instance type.</param>
    /// <param name="instance">The instance object.</param>
    /// <param name="fieldName">The field's name which is to be fetched.</param>
    ///
    /// <returns>The field value from the object.</returns>
    public static T GetField<A, T>(this A self, string fieldName) {
        BindingFlags bindFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            | BindingFlags.Static | BindingFlags.GetField;
        FieldInfo? field = typeof(A).GetField(fieldName, bindFlags);
        if (field == null) {
            var fields = string.Join(", ", typeof(T).GetFields(bindFlags).Select(f => f.Name));
            throw new Exception($"Could not find field '{fieldName}' in {self}\nFields are: {fields}");
        }

        var value = field.GetValue(self);
        if (value is T) {
            return (T)value;
        } else {
            throw new Exception($"'{fieldName}' is not expected type {typeof(T)}");
        }
    }
}
