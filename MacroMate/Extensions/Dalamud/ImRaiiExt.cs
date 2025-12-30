using System;
using Dalamud.Interface.Utility.Raii;

namespace MacroMate.Extensions.Dalamud;

public static class ImRaiiExt {
    public static void Use(this ImRaii.MenuDisposable raii, Action block) { using (raii) if (raii) block(); }
    public static void Use(this ImRaii.ChildDisposable raii, Action block) { using (raii) if (raii) block(); }
    public static void Use(this ImRaii.PopupDisposable raii, Action block) { using (raii) if (raii) block(); }
    public static void Use(this ImRaii.TooltipDisposable raii, Action block) { using (raii) block(); }
    public static void Use(this ImRaii.DisabledDisposable raii, Action block) { using (raii) block(); }
}
