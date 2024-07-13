using MacroMate.MacroTree;
using Sprache;

namespace MacroMateTests;

public class MacroPathTests {
    [Fact]
    public void ParseRoot() {
        var path = MacroPath.ParseText("/");
        path.Segments.Count.ShouldBe(0);
    }

    [Fact]
    public void ParseEmptyAsRoot() {
        var path = MacroPath.ParseText("");
        path.Segments.Count.ShouldBe(0);
    }

    [Fact]
    public void ParseName() {
        var path = MacroPath.ParseText("/My Node");
        path.Segments[0].ShouldBe(new MacroPathSegment.ByName("My Node"));
    }

    [Fact]
    public void ParseNestedName() {
        var path = MacroPath.ParseText("/My Node/Subgroup/My Macro");
        path.Segments[0].ShouldBe(new MacroPathSegment.ByName("My Node"));
        path.Segments[1].ShouldBe(new MacroPathSegment.ByName("Subgroup"));
        path.Segments[2].ShouldBe(new MacroPathSegment.ByName("My Macro"));
    }

    [Fact]
    public void ParseNameWithOffset() {
        var path = MacroPath.ParseText("/My Node@1/Subnode \\@2");
        path.Segments[0].ShouldBe(new MacroPathSegment.ByName("My Node", 1));
        path.Segments[1].ShouldBe(new MacroPathSegment.ByName("Subnode @2"));
    }

    [Fact]
    public void ParseNameWithEscapedAtText() {
        var path = MacroPath.ParseText("/\\@Hello World");
        path.Segments[0].ShouldBe(new MacroPathSegment.ByName("@Hello World"));
    }

    [Fact]
    public void ParseEscapedName() {
        var path = MacroPath.ParseText("""/\/My Node\/\\""");
        path.Segments[0].ShouldBe(new MacroPathSegment.ByName("/My Node/\\"));
    }

    [Fact]
    public void ParseIndex() {
        var path = MacroPath.ParseText("/My Node/@0");
        path.Segments[0].ShouldBe(new MacroPathSegment.ByName("My Node"));
        path.Segments[1].ShouldBe(new MacroPathSegment.ByIndex(0));
    }

    [Fact]
    public void ParseEsacpedIndex() {
        var path = MacroPath.ParseText("""/My Node/\@0""");
        path.Segments[0].ShouldBe(new MacroPathSegment.ByName("My Node"));
        path.Segments[1].ShouldBe(new MacroPathSegment.ByName("@0"));
    }

    [Fact]
    public void ParseNestedOffsetName() {
        var path = MacroPath.ParseText("/My Group/Subgroup@2/@1/Inner");
        path.Segments[0].ShouldBe(new MacroPathSegment.ByName("My Group"));
        path.Segments[1].ShouldBe(new MacroPathSegment.ByName("Subgroup", 2));
        path.Segments[2].ShouldBe(new MacroPathSegment.ByIndex(1));
        path.Segments[3].ShouldBe(new MacroPathSegment.ByName("Inner"));
    }

    [Fact]
    public void ParseComplex() {
        var complex = MacroPath.ParseText("""/A\@\\\//\@B/@1/@2/A""");
        complex.Segments[0].ShouldBe(new MacroPathSegment.ByName("A@\\/"));
        complex.Segments[1].ShouldBe(new MacroPathSegment.ByName("@B"));
        complex.Segments[2].ShouldBe(new MacroPathSegment.ByIndex(1));
        complex.Segments[3].ShouldBe(new MacroPathSegment.ByIndex(2));
        complex.Segments[4].ShouldBe(new MacroPathSegment.ByName("A"));
    }

    [Fact]
    public void ParseShouldErrorOnNoPreceedingStart() {
        Should.Throw<ArgumentException>(() => { MacroPath.ParseText("Hello"); });
    }

    [Fact]
    public void ParseShouldErrorOnNonNumericIndex() {
        Should.Throw<ArgumentException>(() => { MacroPath.ParseText("/@Nope"); });
    }

    [Fact]
    public void ParseShouldErrorOnNonNumericIndexInName() {
        Should.Throw<ArgumentException>(() => { MacroPath.ParseText("/My Group@Nope"); });
    }
}
