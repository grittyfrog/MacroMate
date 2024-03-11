using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using MacroMate.Extensions.Dalamud.Str;
using Xunit.Abstractions;

namespace MacroMateTests;

public class SeStringExTest {
    private readonly ITestOutputHelper output;

    public SeStringExTest(ITestOutputHelper output) {
        this.output = output;
    }

    [Fact]
    public void SplitIntoLines() {
        var input = new SeStringBuilder()
            .Append("Hello\nWorld")
            .Add(new NewLinePayload())
            .Append("Goodbye")
            .Build();

        var splits = input.SplitIntoLines().ToList();
        splits[0].TextValue.ShouldBe("Hello");
        splits[1].TextValue.ShouldBe("World");
        splits[2].TextValue.ShouldBe("Goodbye");
    }

    [Fact]
    public void SplitIntoLinesShouldPreserveEmptyLines() {
        var input = new SeStringBuilder()
            .Append("Hello\n\nWorld")
            .Add(new NewLinePayload())
            .Append("Goodbye")
            .Build();

        var splits = input.SplitIntoLines().ToList();
        splits[0].TextValue.ShouldBe("Hello");
        splits[1].TextValue.ShouldBe("");
        splits[2].TextValue.ShouldBe("World");
        splits[3].TextValue.ShouldBe("Goodbye");
    }

    [Fact]
    public void SplitIntoLinesShouldTreatCRLFAsASingleLine() {
        var input = new SeStringBuilder()
            .Append("Hello\r\nWorld")
            .Build();

        var splits = input.SplitIntoLines().ToList();
        splits[0].TextValue.ShouldBe("Hello");
        splits[1].TextValue.ShouldBe("World");
    }

    [Fact]
    public void SplitIntoLinesShouldNotSplitMixedTextPayloadsThatAreNotNewlines() {
        var input = new SeStringBuilder()
            .Append("Hello\nWorld")
            .Add(new SeHyphenPayload())
            .Append("More text")
            .Add(new NewLinePayload())
            .Append("Goodbye")
            .Build();

        var splits = input.SplitIntoLines().ToList();
        splits[0].TextValue.ShouldBe("Hello");
        splits[1].TextValue.ShouldBe("World–More text");
        splits[2].TextValue.ShouldBe("Goodbye");
    }


    [Fact]
    public void JoinFromLines() {
        var input = new List<SeString> {
            (SeString)"Hello",
            (SeString)"World",
            (SeString)"Goodbye"
        };

        var joined = SeStringEx.JoinFromLines(input);

        output.WriteLine(string.Join(", ", joined.Payloads.Select(p => p.Type)));

        // None of our text payloads should contain newline characters.
        joined.Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("Hello");
        joined.Payloads[1].ShouldBeOfType<NewLinePayload>();
        joined.Payloads[2].ShouldBeOfType<TextPayload>().Text.ShouldBe("World");
        joined.Payloads[3].ShouldBeOfType<NewLinePayload>();
        joined.Payloads[4].ShouldBeOfType<TextPayload>().Text.ShouldBe("Goodbye");


        joined.Payloads.Count.ShouldBe(5); // There shouldn't be any more payloads

        joined.TextValue.ShouldBe("Hello\nWorld\nGoodbye");
    }

    [Fact]
    public void SplitAndJoinNewlinesShouldDoNothing() {
        var input = new SeStringBuilder()
            .Append("Hello\nWorld")
            .Add(new NewLinePayload())
            .Append("Goodbye")
            .Build();

        var split = input.SplitIntoLines();
        var rejoined = SeStringEx.JoinFromLines(split);

        input.TextValue.ShouldBe(rejoined.TextValue);
    }

    [Fact]
    public void NormalizeNewlines() {
        var input = new SeStringBuilder()
            .Append("Hello\nWorld")
            .Add(new NewLinePayload())
            .Append("Goodbye")
            .Build();

        var normalized = input.NormalizeNewlines();

        // None of our text payloads should contain newline characters.
        normalized.Payloads[0].ShouldBeOfType<TextPayload>().Text.ShouldBe("Hello");
        normalized.Payloads[1].ShouldBeOfType<NewLinePayload>();
        normalized.Payloads[2].ShouldBeOfType<TextPayload>().Text.ShouldBe("World");
        normalized.Payloads[3].ShouldBeOfType<NewLinePayload>();
        normalized.Payloads[4].ShouldBeOfType<TextPayload>().Text.ShouldBe("Goodbye");
        normalized.Payloads.Count.ShouldBe(5); // There shouldn't be any more payloads
    }

    [Fact]
    public void NormalizeNewlines_RenormalizeShouldDoNothing() {
        var input = new SeStringBuilder()
            .Append("Hello\nWorld")
            .Add(new SeHyphenPayload())
            .Append("More text")
            .Add(new NewLinePayload())
            .Append("Goodbye")
            .Build();

        var normalized = input.NormalizeNewlines().NormalizeNewlines().NormalizeNewlines();

        // None of our text payloads should contain newline characters.
        var expected = new SeStringBuilder()
            .Append("Hello")
            .Add(new NewLinePayload())
            .Append("World")
            .Add(new SeHyphenPayload())
            .Append("More text")
            .Add(new NewLinePayload())
            .Append("Goodbye")
            .Build();
        normalized.TextValue.ShouldBe(expected.TextValue);
    }
}
