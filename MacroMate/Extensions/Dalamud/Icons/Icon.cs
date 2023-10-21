using System;
using Dalamud.Interface.Internal;

namespace MacroMate.Extensions.Dalamud.Icons;

public record struct Icon(uint Id, IDalamudTextureWrap Texture) : IDisposable {
    public void Dispose() {
        Texture.Dispose();
    }
}
