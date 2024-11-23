using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using MacroMate.Extensions.Dalamud.Str;
using MacroMate.Extensions.Dotnet;
using MacroMate.MacroTree;
using Xunit.Abstractions;

namespace MacroMate.Ipc;

public class MacroChainSupportTest {
    private readonly ITestOutputHelper output;

    public MacroChainSupportTest(ITestOutputHelper output) {
        this.output = output;
    }

    private MacroLink horizontalLink = new MacroLink { Slots = new() { 1, 2 } };
    private MacroLink verticalLink = new MacroLink { Slots = new() { 11, 21 } };

    [Fact]
    public void SmokeTest() {
        var windowsXmlLines = string.Join("", new List<string>() {
            "/echo 1\r\n",
            "/echo 2\r\n",
            "/echo 3\r\n",
            "/echo 4\r\n",
            "/echo 5\r\n",
            "/echo 6\r\n",
            "/echo 7\r\n",
            "/echo 8\r\n",
            "/echo 9\r\n",
            "/echo 10\r\n",
            "/echo 11\r\n",
            "/echo 12\r\n",
            "/echo 13\r\n",
            "/echo 14\r\n",
            "/echo 15\r\n",
            "/echo 16\r\n",
            "/echo 17\r\n",
            "/echo 18\r\n",
        });

        var macroText = SeStringEx.ParseFromText(windowsXmlLines);

        var macroLines = macroText.SplitIntoLines();
        foreach (var (line, index) in macroLines.WithIndex()) {
            this.output.WriteLine($"macroLines[{index}] = {line}");
        }

        var output = MacroChainSupport.MaybeInsertMacroChainCommands(horizontalLink, macroLines);
        foreach (var (line, index) in output.WithIndex()) {
            this.output.WriteLine($"output[{index}] = {line}");
        }

        var outputChunks = output
            .Chunk(15)
            .Select(chunkLines => SeStringEx.JoinFromLines(chunkLines))
            .ToList();

        outputChunks.Count.ShouldBe(2);

        var firstChunkLines = outputChunks[0].SplitIntoLines().ToList();
        foreach (var (line, index) in firstChunkLines.WithIndex()) {
            this.output.WriteLine($"firstChunkLines[{index}] = {line}");
            foreach (var payload in line.Payloads) {
                if (payload is ITextProvider tp) {
                    this.output.WriteLine($"\t{payload.GetType()}: {tp.Text}");
                } else {
                    this.output.WriteLine($"\t{payload.GetType()}");
                }
            }
        }

        firstChunkLines.Count.ShouldBe(15);
        firstChunkLines[0].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 1");
        firstChunkLines[1].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 2");
        firstChunkLines[2].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 3");
        firstChunkLines[3].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 4");
        firstChunkLines[4].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 5");
        firstChunkLines[5].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 6");
        firstChunkLines[6].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 7");
        firstChunkLines[7].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 8");
        firstChunkLines[8].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 9");
        firstChunkLines[9].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 10");
        firstChunkLines[10].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 11");
        firstChunkLines[11].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 12");
        firstChunkLines[12].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 13");
        firstChunkLines[13].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 14");
        firstChunkLines[14].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/nextmacro");

        var secondChunkLines = outputChunks[1].SplitIntoLines().ToList();
        foreach (var (line, index) in secondChunkLines.WithIndex()) {
            this.output.WriteLine($"secondChunkLines[{index}] = {line}");
            foreach (var payload in line.Payloads) {
                if (payload is ITextProvider tp) {
                    this.output.WriteLine($"\t{payload.GetType()}: {tp.Text}");
                } else {
                    this.output.WriteLine($"\t{payload.GetType()}");
                }
            }
        }

        secondChunkLines.Count.ShouldBe(4);
        secondChunkLines[0].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 15");
        secondChunkLines[1].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 16");
        secondChunkLines[2].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 17");
        secondChunkLines[3].Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("/echo 18");
    }
}
