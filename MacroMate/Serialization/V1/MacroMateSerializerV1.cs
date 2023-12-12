using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using MacroMate.MacroTree;

namespace MacroMate.Serialization.V1;

public static class MacroMateSerializerV1 {
    private static XmlSerializer xmlSerializer = new XmlSerializer(typeof(MacroMateV1XML));

    public static string Export(MateNode tree) {
        var xml = MacroMateV1XML.From(tree);

        using (var stringWriter = new StringWriter()) {
            xmlSerializer.Serialize(stringWriter, xml);

            var xmlBytes = Encoding.Unicode.GetBytes(stringWriter.ToString());
            var xmlBase64 = Convert.ToBase64String(xmlBytes);
            return xmlBase64;
        }
    }

    public static MateNode Import(string importString) {
        try {
            var xmlBytes = Convert.FromBase64String(importString);
            using (var xmlStream = new MemoryStream(xmlBytes)) {
                var xml = (MacroMateV1XML)xmlSerializer.Deserialize(xmlStream)!;
                return xml.ToReal();
            }
        } catch (Exception) {
            Env.ChatGui.PrintError("Could not import Macro Mate tree");
            throw;
        }
    }

    public static void Write(MateNode root, FileInfo file) {
        var rootXml = MacroMateV1XML.From(root);

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
