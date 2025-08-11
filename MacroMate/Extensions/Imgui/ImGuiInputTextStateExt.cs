using System.Linq;
using Dalamud.Bindings.ImGui;

namespace MacroMate.Extensions.Imgui;

public static class ImGuiInputTextStateExt {
    public static string SelectedText(this ImGuiInputTextStatePtr self) {
        // These are in wchar-positions, not UTF8 positions
        var lower = self.Stb.SelectStart <= self.Stb.SelectEnd ? self.Stb.SelectStart : self.Stb.SelectEnd;
        var higher = self.Stb.SelectStart <= self.Stb.SelectEnd ? self.Stb.SelectEnd : self.Stb.SelectStart;
        var selectionLength = higher - lower;
        var selectedChars = self.TextW.AsEnumerable().Skip(lower).Take(selectionLength);
        return string.Join("", selectedChars.ToString()); 
    }
    
    /// <summary>
    /// Returns the current "word" the user is writing, based on the characters from the previous
    /// word boundary up to the cursor.
    /// </summary>
    public static string? CurrentEditWord(this ImGuiInputTextStatePtr self) {
        var (wordStart, wordEnd) = self.CurrentEditWordBounds();
        var length = wordEnd - wordStart;

        if (wordStart == wordEnd) { return null; }

        return string.Join("", self.TextW.AsEnumerable().Skip(wordStart).Take(length).Select(us => (char)us));
    }

    public static (int, int) CurrentEditWordBounds(this ImGuiInputTextStatePtr self) {
        var wordStart = self.CurrentEditWordStart();
        var wordEnd = self.Stb.Cursor;
        return (wordStart, wordEnd);
    }

    public static int CurrentEditWordStart(this ImGuiInputTextStatePtr self) {
        var wordStart = self.Stb.Cursor;
        while (wordStart > 0 && !IsWordBoundary((char)self.TextW[wordStart-1])) {
            wordStart -= 1;
        }
        return wordStart;
    }

    private static bool IsWordBoundary(char c) {
        return char.IsSeparator(c) || char.IsWhiteSpace(c) || c == '\uE040' || c == '\uE041'; 
    }
}
