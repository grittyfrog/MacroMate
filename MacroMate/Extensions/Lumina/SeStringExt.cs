using System.Linq;
using MacroMate.Extensions.Dotnet;
using Lumina.Text;
using Lumina.Text.Payloads;

namespace MacroMate.Extensions.Lumina;

public static class SeStringExt {
    public static string Text(this SeString self) =>
        string.Join(
            " ",
            self.Payloads
                .Select(payload => (payload as TextPayload)?.RawString)
                .WithoutNull()
        );
}
