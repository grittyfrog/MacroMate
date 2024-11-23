using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Text.SeStringHandling;
using MacroMate.Extensions.Dalamud;
using MacroMate.Extensions.Dotnet;
using MacroMate.MacroTree;

namespace MacroMate.Ipc;

// This isn't really IPC but I didn't want to make another folder
public static class MacroChainSupport {
    /// <summary>
    /// Maybe insert `/nextmacro` or `/nextmacro down`
    /// </summary>
    public static IEnumerable<SeString> MaybeInsertMacroChainCommands(
        MacroLink link,
        IEnumerable<SeString> lines
    ) {
        // We want a continuous "Snake" of right/down Links to be able to use Macro Chain. For example:
        //
        // - 1 2 3 is fine
        // - 1 3 5 is not fine (not continuous)
        // - 0 10 20 is fine (continuous downwards)
        // - 0 1 11 is fine (continous right/down)
        // - 3 2 1 is not fine (going left)
        //
        // If all of our links don't meet this criteria then there's no point and this setting should be ignored.

        var newLines = new List<SeString>();
        var lineChunks = lines.Chunk(14).Lookahead(2);
        var linkSlots = link.Slots.Lookahead(2);
        foreach (var (lineChunkWindow, linkWindow) in lineChunks.ZipWithDefault(linkSlots)) {
            if (lineChunkWindow == null || lineChunkWindow.Count == 0) { break; } // No more lines to work on
            var lineChunk = lineChunkWindow[0];

            if (linkWindow == null) { // No more links, which means there's nothing to /nextmacro to
                newLines.AddRange(lineChunk);
                continue;
            }

            // We are at the end of the link pairs, which means there's nothing to /nextmacro to
            if (linkWindow.Count < 2) {
                newLines.AddRange(lineChunk);
                continue;
            }

            // We only need to add /nextmacro or /nextmacro down if the next macro block has any lines
            var nextLineChunks = lineChunkWindow.ElementAtOrDefault(1);
            if (nextLineChunks == null || nextLineChunks.Count() == 0) {
                newLines.AddRange(lineChunk);
                continue;
            }

            var (currentLink, nextLink) = (linkWindow[0], linkWindow[1]);
            if (currentLink + 1 == nextLink) { // Right Link
                newLines.AddRange(lineChunk);
                newLines.Add("/nextmacro");
            } else if (currentLink + 10 == nextLink) { // Down Link
                newLines.AddRange(lineChunk);
                newLines.Add("/nextmacro down");
            } else { // Non-continuous link -- abort!
                return lines;
            }
        }

        return newLines;
    }
}
