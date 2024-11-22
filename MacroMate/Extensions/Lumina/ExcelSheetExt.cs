using Lumina.Excel;

namespace MacroMate.Extensions.Lumina;

public static class ExcelSheetExt {
    public static T? GetRowOrNull<T>(this ExcelSheet<T> self, uint rowId) where T : struct, IExcelRow<T> {
        if (self.TryGetRow(rowId, out var row)) { return row; }
        return null;
    }
}
