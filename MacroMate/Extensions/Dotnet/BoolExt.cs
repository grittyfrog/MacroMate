namespace MacroMate.Extensions.Dotnet;

public static class BoolExt {
    public static int ToInt(this bool self) => self ? 1 : 0;
}
