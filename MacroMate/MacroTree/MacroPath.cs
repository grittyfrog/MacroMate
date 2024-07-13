using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Sprache;

namespace MacroMate.MacroTree;

/// <summary>
/// A path into the MacroTree that may point to a node (either a group or macro)
///
/// - Paths start with a `/` which indicates the root node of the macro config tree
/// - Paths are formed of segments separated by `/`
/// - By default a segment identifies a node by name
/// - A segment that starts with `@` followed by a number identifies a node by index (starting from 0)
/// - The `@` and `/` character can be escaped by `\` which will treat it as a literal character.
/// - A `@` at the start of a segment can be escaped using `\` which treats it as a literal character
/// - A `\` can be escaped using `\\`
///
/// Syntax Examples:
///
///    ``                       The empty string, treated the same as `/`.
///    `/`                      Select the root node
///    `/My Group`              Select the group called 'My Group' under the root node
///    `/My Group/Subgroup 1`
///    `/@0`                    Select the first child of root
///    `/My Group/@1`           Select the second child of 'My Group'
///    `/My Group/\@1`          Select a macro named '@1' in 'My Group'
///    `/My Group@1             Select the second macro named 'My Group' under the root node
///
/// Invalid syntax:
///
///     `///`                   All segments must have at least one character (excepting the root path)
///     `//My segment/`         See above
/// </summary>
public record class MacroPath(
    ImmutableList<MacroPathSegment> Segments
) {
    /// <summary>
    /// Parse a string into a macro path
    /// </summary>
    public static MacroPath ParseText(string path) {
        try {
            return new MacroPath(MacroPathSegment.ParseText(path).ToImmutableList());
        } catch (ParseException ex) {
            throw new ArgumentException($"invalid macro path '{path}': {ex.Message}", ex);
        }
    }
}

public interface MacroPathSegment {
    public record class ByName(string name, int offset = 0) : MacroPathSegment {}
    public record class ByIndex(int index) : MacroPathSegment {}

    public static IEnumerable<MacroPathSegment> ParseText(string path) {
        if (path == "") { return ImmutableList<MacroPathSegment>.Empty; }
        if (path == "/") { return ImmutableList<MacroPathSegment>.Empty; }
        if (!path.StartsWith("/")) { throw new ArgumentException("MacroPath must start with '/'"); }

        return parseMacroPathSegment.XMany().End().Parse(path);
    }

    // Reads `\\` as a literal `\`
    // Reads `\@` as a literal `@`
    // Reads `\/` as a literal `/`
    private static readonly Parser<char> parseSegmentChar =
        Parse.String("\\\\").Select(_ => '\\')
          .Or(Parse.String("\\@").Select(_ => '@'))
          .Or(Parse.String("\\/").Select(_ => '/'))
          .Or(Parse.CharExcept(new [] { '/', '@' }));

    private static readonly Parser<MacroPathSegment.ByIndex> parseMacroPathSegmentByIndex =
        from _ in Parse.Char('@')
        from index in Parse.Number
        select new MacroPathSegment.ByIndex(int.Parse(index));

    private static readonly Parser<MacroPathSegment.ByName> parseMacroPathSegmentByName =
        from name in parseSegmentChar.AtLeastOnce().Text()
        from offset in parseMacroPathSegmentByIndex.XOptional()
        select new MacroPathSegment.ByName(name, offset.GetOrDefault()?.index ?? 0);

    private static readonly Parser<MacroPathSegment> parseMacroPathSegment =
        from _ in Parse.Char('/')
        from segment in parseMacroPathSegmentByIndex.Or<MacroPathSegment>(parseMacroPathSegmentByName)
        select segment;
}
