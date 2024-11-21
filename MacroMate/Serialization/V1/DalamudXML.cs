using System.Xml;
using System.Xml.Serialization;
using Lumina.Excel;
using MacroMate.Extensions.Dalamaud.Excel;

namespace MacroMate.Serialization.V1;

public class ExcelIdXML {
    public ExcelIdXML() { Id = 0; }

    public ExcelIdXML(ExcelId excelId) {
        Comment = new XmlDocument { XmlResolver = null }.CreateComment(excelId.DisplayName());
        Id = excelId.Id;
    }

    public ExcelId<T> ToReal<T>() where T : struct, IExcelRow<T> => new ExcelId<T>(Id);

    [XmlIgnore]
    public XmlComment? Comment { get; set; } = null;

    [XmlText]
    public uint Id { get; set; }
}
