using System;
using Dalamud.Interface.Utility.Raii;

namespace MacroMate.Extensions.Dalamud;

public static class ImRaiiExt {
    public static void Use(this ImRaii.IEndObject raii, Action block) {
        using (var draw = raii) {
            if (draw) {
                block();
            }
        }
    }
}
