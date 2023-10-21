using System.IO;
using System.Xml.Serialization;
using MacroMate.MacroTree;

namespace MacroMate.Serialization.V1;

public static class SaveManagerV1 {
    public static void Write(MateNode root, FileInfo file) {
        var rootXml = MacroMateV1XML.From(root);
        var xmlSerializer = new XmlSerializer(typeof(MacroMateV1XML));

        using (var writeFile = file.Create())
        using (var writer = new StreamWriter(writeFile)) {
            xmlSerializer.Serialize(writer, rootXml);
            writer.Flush();
        }
    }

    public static MateNode Read(FileInfo file) {
        using (var readerFile = file.OpenRead())
        using (var reader = new StreamReader(readerFile)) {
            try {
                var xmlSerializer = new XmlSerializer(typeof(MacroMateV1XML));
                var rootXml = (MacroMateV1XML)xmlSerializer.Deserialize(reader)!;
                return rootXml.ToReal();
            } catch (InvalidDataException) {
                Env.ChatGui.PrintError($"Could not load Macro Mate Config");
                throw;
            }
        }
    }
}
