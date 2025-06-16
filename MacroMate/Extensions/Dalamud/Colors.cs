using System.Numerics;

namespace MacroMate.Extensions.Dalamud;

public static class Colors {
    public static Vector4 White = new(255.0f, 255.0f, 255.0f, 1.0f);

    public static Vector4 FinalFantasyBlue = new(0.102f, 0.0f, 0.427f, 1.0f);

    public static Vector4 RaptorBlue = new(
        91.0f / 255.0f,
        127.0f / 255.0f,
        156.0f / 255.0f,
        1.0f
    ); // #5B7FC0

    public static Vector4 SkyBlue = new(
        131.0f / 255.0f,
        176.0f / 255.0f,
        210.0f / 255.0f,
        1.0f
    ); // 83B0D2

    public static Vector4 ActiveGreen = new(
        115.0f / 255.0f,
        231.0f / 255.0f,
        156.0f / 255.0f,
        1.0f
    );

    public static Vector4 ErrorRed = new(
        255.0f / 255.0f,
        0.0f / 255.0f,
        0.0f / 255.0f,
        1.0f
    );

    public static Vector4 WarningYellow = new(
        255.0f / 255.0f,
        255.0f / 255.0f,
        0.0f / 255.0f,
        1.0f
    );

    public static Vector4 AutoTranslateStartGreen = new(
        96.0f / 255.0f,
        223.0f / 255.0f,
        46.0f / 255.0f,
        1.0f
    ); // #60DF2E

    public static Vector4 AutoTranslateEndRed = new(
        221 / 255.0f,
        54 / 255.0f,
        54 / 255.0f,
        1.0f
    ); // #DD3636

    public static Vector4 HighlightGold = new(1.0f, 0.8f, 0.0f, 1.0f);
}
