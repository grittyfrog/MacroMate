using System.Xml.Serialization;
using Dalamud.Game.ClientState.Objects.Enums;

namespace MacroMate.Serialization.V1;

/// <summary>
/// Stable XML serialization mirror of <see cref="ObjectKind"/>.
/// Decouples serialized XML from upstream enum renames.
/// Uses exhaustive switch mappings so upstream changes cause compile errors.
/// </summary>
public enum ObjectKindXML {
    None = ObjectKind.None,
    Pc = ObjectKind.Pc,
    BattleNpc = ObjectKind.BattleNpc,
    EventNpc = ObjectKind.EventNpc,
    Treasure = ObjectKind.Treasure,
    Aetheryte = ObjectKind.Aetheryte,
    GatheringPoint = ObjectKind.GatheringPoint,
    EventObj = ObjectKind.EventObj,
    Mount = ObjectKind.Mount,
    Companion = ObjectKind.Companion,
    Retainer = ObjectKind.Retainer,
    AreaObject = ObjectKind.AreaObject,
    HousingEventObject = ObjectKind.HousingEventObject,
    Cutscene = ObjectKind.Cutscene,
    ReactionEventObject = ObjectKind.ReactionEventObject,
    Ornament = ObjectKind.Ornament,
    CardStand = ObjectKind.CardStand,

    // Old Dalamud names (before FFXIVClientStructs migration).
    // These are aliases so old XML can still be deserialized.
    [XmlEnum("Player")]
    Player = Pc,
    [XmlEnum("MountType")]
    MountType = Mount,
    [XmlEnum("Area")]
    Area = AreaObject,
    [XmlEnum("Housing")]
    Housing = HousingEventObject,
}

public static class ObjectKindXMLExtensions {
    public static ObjectKindXML ToXml(this ObjectKind value) => (ObjectKindXML)value;
    public static ObjectKind FromXml(this ObjectKindXML value) => (ObjectKind)value;
}
