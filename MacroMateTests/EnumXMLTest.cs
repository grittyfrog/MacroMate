using System.Reflection;
using System.Xml.Serialization;
using MacroMate.Serialization.V1;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Conditions;

namespace MacroMate.Serialization;

/// <summary>
/// Helper classes for XML serialization tests since XmlSerializer needs a concrete type.
/// </summary>
public class ObjectKindXMLWrapper {
    public ObjectKindXML? Value { get; set; }
}

public class ConditionFlagXMLWrapper {
    public ConditionFlagXML Value { get; set; }
}

public class EnumXMLTest {
    private static T Deserialize<T>(string xml) {
        var serializer = new XmlSerializer(typeof(T));
        using var reader = new StringReader(xml);
        return (T)serializer.Deserialize(reader)!;
    }

    /// <summary>
    /// Gets the XML serialization name for each member of an enum, including aliases.
    /// Members with [XmlEnum("Name")] use that name; others use the member name directly.
    /// </summary>
    private static IEnumerable<(string xmlName, TEnum value)> GetAllXmlNames<TEnum>() where TEnum : struct, Enum {
        foreach (var name in Enum.GetNames<TEnum>()) {
            var field = typeof(TEnum).GetField(name)!;
            var xmlEnumAttr = field.GetCustomAttribute<XmlEnumAttribute>();
            var xmlName = xmlEnumAttr?.Name ?? name;
            yield return (xmlName, Enum.Parse<TEnum>(name));
        }
    }

    // --- ObjectKindXML ---

    [Fact]
    public void ObjectKindXML_DeserializesAllNames() {
        foreach (var (xmlName, expected) in GetAllXmlNames<ObjectKindXML>()) {
            var xml = $"""
                <?xml version="1.0" encoding="utf-16"?>
                <ObjectKindXMLWrapper>
                  <Value>{xmlName}</Value>
                </ObjectKindXMLWrapper>
                """;

            var result = Deserialize<ObjectKindXMLWrapper>(xml);
            result.Value.ShouldBe(expected);
        }
    }

    [Fact]
    public void ObjectKindXML_MapsAllUpstreamValues() {
        foreach (var upstream in Enum.GetValues<ObjectKind>()) {
            var xmlVal = upstream.ToXml();
            Enum.IsDefined(xmlVal).ShouldBeTrue($"ObjectKindXML is missing a member for ObjectKind.{upstream}");
            var backToUpstream = xmlVal.FromXml();
            backToUpstream.ShouldBe(upstream);
        }
    }

    // --- ConditionFlagXML ---

    [Fact]
    public void ConditionFlagXML_DeserializesAllNames() {
        foreach (var (xmlName, expected) in GetAllXmlNames<ConditionFlagXML>()) {
            var xml = $"""
                <?xml version="1.0" encoding="utf-16"?>
                <ConditionFlagXMLWrapper>
                  <Value>{xmlName}</Value>
                </ConditionFlagXMLWrapper>
                """;

            var result = Deserialize<ConditionFlagXMLWrapper>(xml);
            result.Value.ShouldBe(expected);
        }
    }

    [Fact]
    public void ConditionFlagXML_MapsAllUpstreamValues() {
        foreach (var upstream in Enum.GetValues<ConditionFlag>()) {
            var xmlVal = upstream.ToXml();
            Enum.IsDefined(xmlVal).ShouldBeTrue($"ConditionFlagXML is missing a member for ConditionFlag.{upstream}");
            var backToUpstream = xmlVal.FromXml();
            backToUpstream.ShouldBe(upstream);
        }
    }
}
