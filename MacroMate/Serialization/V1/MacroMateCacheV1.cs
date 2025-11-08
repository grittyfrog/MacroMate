using System;
using System.IO;
using System.Xml.Serialization;
using MacroMate.Cache;

namespace MacroMate.Serialization.V1;

public static class MacroMateCacheV1 {
    private static XmlSerializer cacheXmlSerializer = new XmlSerializer(typeof(MacroMateCacheXML));

    public static void Write(MacroMateCache cache, FileInfo file) {
        var cacheXml = MacroMateCacheXML.From(cache);

        using (var writeFile = file.Create())
        using (var writer = new StreamWriter(writeFile)) {
            cacheXmlSerializer.Serialize(writer, cacheXml);
            writer.Flush();
        }
    }

    public static MacroMateCache Read(FileInfo file) {
        using (var readerFile = file.OpenRead())
        using (var reader = new StreamReader(readerFile)) {
            try {
                var cacheXml = (MacroMateCacheXML)cacheXmlSerializer.Deserialize(reader)!;
                return cacheXml.ToReal();
            } catch (InvalidDataException) {
                Env.ChatGui.PrintError($"Could not load Macro Mate Cache");
                throw;
            }
        }
    }
}
