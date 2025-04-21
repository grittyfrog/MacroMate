using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Utility;
using MacroMate.MacroTree;

namespace MacroMate.Serialization.V1;

public static class MacroMateSerializerV1 {
    private static XmlAttributeOverrides xmlAttributeOverrides = new XmlAttributeOverrides()
        .AllowObsoleteAttributeSerialization();

    private static XmlSerializer configXmlSerializer = new XmlSerializer(typeof(MacroMateV1XML), xmlAttributeOverrides);
    private static XmlSerializer mateNodeXmlSerializer = new XmlSerializer(typeof(MateNodeXML), xmlAttributeOverrides);

    public static string Export(MateNode tree) {
        var xml = MateNodeXML.From(tree);

        using (var stringWriter = new StringWriter()) {
            mateNodeXmlSerializer.Serialize(stringWriter, xml);
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
                var xml = (MateNodeXML)mateNodeXmlSerializer.Deserialize(xmlStream)!;
                return xml.ToReal();
            }
        } catch (Exception e) {
            Env.PluginLog.Error(e, "Could not import Macro Mate preset");
            return null;
        }
    }

    public static void Write(MacroConfig config, FileInfo file) {
        var configXml = MacroMateV1XML.From(config);

        using (var writeFile = file.Create())
        using (var writer = new StreamWriter(writeFile)) {
            configXmlSerializer.Serialize(writer, configXml);
            writer.Flush();
        }
    }

    public static MacroConfig Read(FileInfo file) {
        using (var readerFile = file.OpenRead())
        using (var reader = new StreamReader(readerFile)) {
            try {
                var rootXml = (MacroMateV1XML)configXmlSerializer.Deserialize(reader)!;
                return rootXml.ToReal();
            } catch (InvalidDataException) {
                Env.ChatGui.PrintError($"Could not load Macro Mate Config");
                throw;
            }
        }
    }

    /// <summary>
    /// By default XmlSerializer will refuse to serialize fields/values marked [Obsolete]. This causes issues
    /// when upgrading Dalamud since our config may have an "old" name that will fail to serialize and cause
    /// Macro Mate not to load.
    /// </summary>
    private static XmlAttributeOverrides AllowObsoleteAttributeSerialization(this XmlAttributeOverrides self) {
        var enumClasses = new[] { typeof(ConditionFlag) };
        var dontIgnore = new XmlAttributes { XmlIgnore = false };
        foreach (var enumClass in enumClasses) {
            var obsoleteValues = enumClass
              .GetFields(BindingFlags.Public | BindingFlags.Static)
              .Where(fieldInfo => fieldInfo.GetCustomAttribute(typeof (ObsoleteAttribute)) != null);

            foreach (var obsoleteValue in obsoleteValues) {
                self.Add(enumClass, obsoleteValue.Name, dontIgnore);
            }
        }

        return self;
    }
}
