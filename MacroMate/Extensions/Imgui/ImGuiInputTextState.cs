using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Interface.Utility;
using ImGuiNET;

namespace MacroMate.Extensions.Imgui;

/// Stolen and butchered from https://github.com/goatcorp/Dalamud/blob/7ee20272deb187908c2b4d3d4a837a7127f7abf4/Dalamud/Interface/Internal/DalamudIme.cs#L883
///
/// All this to get the scroll state...
///
/// Thanks Dalamud!
[StructLayout(LayoutKind.Sequential)]
public unsafe struct ImGuiInputTextState
{
    public static int TextStateOffset = 0x4588;

    public uint Id;
    public int CurLenW;
    public int CurLenA;
    public ImVector<char> TextWRaw;
    public ImVector<byte> TextARaw;
    public ImVector<byte> InitialTextARaw;
    public bool TextAIsValid;
    public int BufCapacityA;
    public float ScrollX;
    public StbTextEditState Stb;
    public float CursorAnim;
    public bool CursorFollow;
    public bool SelectedAllMouseLock;
    public bool Edited;
    public ImGuiInputTextFlags Flags;

    public ImVectorWrapper<char> TextW => new((ImVector*)&this.ThisPtr->TextWRaw);

    public (int Start, int End, int Cursor) SelectionTuple
    {
        get => (this.Stb.SelectStart, this.Stb.SelectEnd, this.Stb.Cursor);
        set => (this.Stb.SelectStart, this.Stb.SelectEnd, this.Stb.Cursor) = value;
    }

    private ImGuiInputTextState* ThisPtr => (ImGuiInputTextState*)Unsafe.AsPointer(ref this);

    public string SelectedText() {
        // These are in wchar-positions, not UTF8 positions
        var lower = Stb.SelectStart <= Stb.SelectEnd ? Stb.SelectStart : Stb.SelectEnd;
        var higher = Stb.SelectStart <= Stb.SelectEnd ? Stb.SelectEnd : Stb.SelectStart;
        var selectionLength = higher - lower;
        var selectedChars = TextW.DataSpan.Slice(lower, selectionLength);
        return selectedChars.ToString(); // ToString on Span<char> constructs the string
    }

    /// <summary>
    /// Returns the current "word" the user is writing, based on the characters from the previous
    /// word boundary up to the cursor.
    /// </summary>
    public string? CurrentEditWord() {
        var (wordStart, wordEnd) = CurrentEditWordBounds();
        var length = wordEnd - wordStart;

        if (wordStart == wordEnd) { return null; }
        return TextW.DataSpan.Slice(wordStart, length).ToString();
    }

    public (int, int) CurrentEditWordBounds() {
        var wordStart = CurrentEditWordStart();
        var wordEnd = Stb.Cursor;
        return (wordStart, wordEnd);
    }

    public int CurrentEditWordStart() {
        var wordStart = Stb.Cursor;
        while (wordStart > 0 && !IsWordBoundary(TextW.DataSpan[wordStart-1])) {
            wordStart -= 1;
        }
        return wordStart;
    }

    private bool IsWordBoundary(char c) {
        return char.IsSeparator(c) || char.IsWhiteSpace(c) || c == '\uE040' || c == '\uE041'; 
    }
}

/// <summary>
/// Ported from imstb_textedit.h.
/// </summary>
[StructLayout(LayoutKind.Sequential, Size = 0xE2C)]
public struct StbTextEditState
{
    /// <summary>
    /// Position of the text cursor within the string.
    /// </summary>
    public int Cursor;

    /// <summary>
    /// Selection start point.
    /// </summary>
    public int SelectStart;

    /// <summary>
    /// selection start and end point in characters; if equal, no selection.
    /// </summary>
    /// <remarks>
    /// Note that start may be less than or greater than end (e.g. when dragging the mouse,
    /// start is where the initial click was, and you can drag in either direction.)
    /// </remarks>
    public int SelectEnd;

    /// <summary>
    /// Each text field keeps its own insert mode state.
    /// To keep an app-wide insert mode, copy this value in/out of the app state.
    /// </summary>
    public byte InsertMode;

    /// <summary>
    /// Page size in number of row.
    /// This value MUST be set to >0 for pageup or pagedown in multilines documents.
    /// </summary>
    public int RowCountPerPage;

    // Remainder is stb-private data.
    public byte CursorAtEndOfLine; // not implemented yet
    public byte Initialized;
    public byte HasPreferredX;
}
