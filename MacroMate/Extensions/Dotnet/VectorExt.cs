using System.Numerics;

namespace MacroMate.Extensions.Dotnet;

public static class VectorExt {
    public static Vector2 Scale(this Vector2 vec, float scale) =>
        new Vector2(vec.X * scale, vec.Y * scale);
}
