using System;
using System.Collections.Generic;
using System.Reflection;

namespace MacroMate.Extensions.Dotnet;

public static class EnumExt {
    public static bool IsObsolete(Enum self) {
        var fieldInfo = self.GetType().GetField(self.ToString());
        var attributes = (ObsoleteAttribute[])fieldInfo!.GetCustomAttributes(typeof(ObsoleteAttribute), false);
        return (attributes != null && attributes.Length > 0);
    }

    /// <summary>
    /// Generates the list of Value => Name mappings for an Enum, preferring not to use [Obsolete] names.
    ///
    /// This uses reflection, so prefer to cache it somewhere.
    /// </summary>
    public static Dictionary<T, string> BuildNameMap<T>() where T : System.Enum {
        var enumToNonObsolete = new Dictionary<FieldInfo, string>();
        var enumToObsolete = new Dictionary<FieldInfo, string>();

        var enumFields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static);
        foreach (var enumField in enumFields) {
            var isObsolete = enumField.GetCustomAttribute(typeof(ObsoleteAttribute)) != null;
            if (!isObsolete) {
                enumToNonObsolete.TryAdd(enumField, enumField.Name);
            } else {
                enumToObsolete.TryAdd(enumField, enumField.Name);
            }
        }

        var nameMap = new Dictionary<T, string>();
        foreach (var dict in new[] { enumToNonObsolete, enumToObsolete }) {
            foreach (var kv in dict) {
                var enumValue = (T)kv.Key.GetValue(null)!;
                nameMap.TryAdd(enumValue, kv.Value);
            }
        }

        return nameMap;
    }
}
