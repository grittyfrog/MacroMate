using System.IO;
using Markdig.Renderers.Roundtrip;
using Markdig.Syntax;

namespace MacroMate.Extensions.Markdig;

public static class MarkdownExt {
    public static string ToRawLines(this LeafBlock leaf) {
        var sw = new StringWriter();
        var rrRenderer = new RoundtripRenderer(sw);
        rrRenderer.WriteLeafRawLines(leaf);
        return sw.ToString();
    }
}
