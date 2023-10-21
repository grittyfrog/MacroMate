using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Client.System.String;

namespace MacroMate.Extensions.Dalamud;

public static class Utf8StringEx {
    public unsafe static string MarshalString(Utf8String* utf8String) {
      var str = Marshal.PtrToStringUTF8((IntPtr)utf8String->StringPtr);
      return str ?? throw new Exception("Could not marshal UTF8 Stringn");
    }
}
