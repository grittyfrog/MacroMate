using System;
using System.IO;
using System.Xml.Serialization;
using Dalamud.Utility;
using MacroMate.MacroTree;

namespace MacroMate.Serialization.V1;

public static class MacroMateSerializerV1 {
    private static XmlSerializer xmlSerializer = new XmlSerializer(typeof(MacroMateV1XML));

    public static string Export(MateNode tree) {
        var xml = MacroMateV1XML.From(tree);

        using (var stringWriter = new StringWriter()) {
            xmlSerializer.Serialize(stringWriter, xml);
            var xmlString = stringWriter.ToString();

            var compressed = Util.CompressString(xmlString);
            var compressedBase64 = Convert.ToBase64String(compressed);
            return compressedBase64;
        }
    }

    public static MateNode? Import(string compressedBase64) {
        try {
            var compressed = Convert.FromBase64String(compressedBase64);
            var xmlString = Util.DecompressString(compressed);
            using (var xmlStream = new StringReader(xmlString)) {
                var xml = (MacroMateV1XML)xmlSerializer.Deserialize(xmlStream)!;
                return xml.ToReal();
            }
        } catch (Exception e) {
            Env.PluginLog.Error(e, "Could not import Macro Mate preset");
            return null;
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
