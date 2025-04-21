using System.Collections.Generic;
using System.Linq;
using Generator.Equals;
using Lumina.Excel.Sheets;
using MacroMate.Extensions.Dalamaud.Excel;
using MacroMate.Extensions.Dalamud;

namespace MacroMate.Conditions;

public record class PlayerStatusCondition(
    List<WellFedOrMedicated> Statuses
) : IValueCondition {
    private const uint WELL_FED = 48;
    private const uint MEDICATED = 49;

    public PlayerStatusCondition(WellFedOrMedicated status) : this(new List<WellFedOrMedicated>() { status }) {}

    public string ValueName {
        get {
            if (Statuses.Count == 0) { return "<no statuses>"; }
            if (Statuses.Count == 1) { return Statuses.First().ValueName; }
            return string.Join("\n", Statuses.Select(s => s.ValueName));
        }
    }

    public string NarrowName {
        get {
            if (Statuses.Count == 0) { return "<no statuses>"; }
            if (Statuses.Count == 1) { return Statuses.First().NarrowName; }
            return string.Join("\n", Statuses.Select(s => s.NarrowName));
        }
    }

    public bool SatisfiedBy(ICondition other) {
        var otherStatusCondition = other as PlayerStatusCondition;
        if (otherStatusCondition == null) { return false; }

        // We are satisfied if all our status effects are satisfied by any effect in [other]
        //
        // Player Status: [Rronek Steak, Medicine, Inner Beast]
        // Condition: [Medicine]
        //
        // Status = [Medicine]
        // Player Status: otherStatusCondition
        //
        // [Medicine].All(status => [Steak, Medicine, Beast].Any(smb => [Medicine].SatisfiedBy([Medicine])))
        return Statuses.All(status => otherStatusCondition.Statuses.Any(os => status.SatisfiedBy(os)));
    }

    public static PlayerStatusCondition? Current() {
        if (Env.ClientState.LocalPlayer == null) { return null; }

        var playerStatuses = Env.ClientState.LocalPlayer.StatusList.Select(s => {
            if (s.StatusId == WELL_FED || s.StatusId == MEDICATED) {
                var itemFoodId = s.Param;
                var itemInfo = Env.ItemIndex.FindFoodItemInfo(itemFoodId);
                if (itemInfo == null) { return null; }
                return new WellFedOrMedicated(
                    ConsumableType: itemInfo.ItemType,
                    Item: new ExcelId<Item>(itemInfo.ItemId),
                    IsHighQuality: itemInfo.IsHighQuality
                );
            } 

            return null;
        }).OfType<WellFedOrMedicated>();

        return new PlayerStatusCondition(playerStatuses.ToList());
    }

    public static IValueCondition.IFactory Factory => new ConditionFactory();
    public IValueCondition.IFactory FactoryRef => Factory;

    class ConditionFactory : IValueCondition.IFactory {
        public string ConditionName => "Player Status";
        public string ExpressionName => "Player.Status";

        public IValueCondition? Current() => PlayerStatusCondition.Current();
        public IValueCondition Default() => new PlayerStatusCondition(new List<WellFedOrMedicated>());

        public IValueCondition? FromConditions(CurrentConditions conditions) {
            return conditions.playerStatusCondition;
        }

        public IEnumerable<IValueCondition> TopLevel() {
            return new[] {
                new PlayerStatusCondition(
                    new WellFedOrMedicated(
                        ConsumableType: ItemInfo.Type.FOOD,
                        Item: new ExcelId<Item>(0),
                        IsHighQuality: false
                    )
                ),
                new PlayerStatusCondition(
                    new WellFedOrMedicated(
                        ConsumableType: ItemInfo.Type.MEDICINE,
                        Item: new ExcelId<Item>(0),
                        IsHighQuality: false
                    )
                )
            };
        }

        public IEnumerable<IValueCondition> Narrow(IValueCondition search) {
            var statusCondition = search as PlayerStatusCondition;
            if (statusCondition == null) { return new List<IValueCondition>(); }

            // If we don't have exactly 1 status then we can't narrow
            if (statusCondition.Statuses.Count != 1) { return new List<IValueCondition>(); }
            var fedOrMed = statusCondition.Statuses[0];

            // If we already have a food we can't narrow any further
            if (fedOrMed.Item.Id != 0) { return new List<IValueCondition>(); }

            // Otherwise, we can narrow to specific food items
            return Env.DataManager.GetExcelSheet<ItemFood>()
                .Select(food => Env.ItemIndex.FindFoodItemInfo(food.RowId))
                .OfType<ItemInfo>()
                .Where(itemInfo => itemInfo.ItemType == fedOrMed.ConsumableType)
                .SelectMany(itemInfo =>
                    new[] {
                        new PlayerStatusCondition(
                            new List<WellFedOrMedicated> {
                                new WellFedOrMedicated(
                                    ConsumableType: itemInfo.ItemType,
                                    Item: new ExcelId<Item>(itemInfo.ItemId),
                                    IsHighQuality: true
                                )
                            }
                        ),
                        new PlayerStatusCondition(
                            new List<WellFedOrMedicated> {
                                new WellFedOrMedicated(
                                    ConsumableType: itemInfo.ItemType,
                                    Item: new ExcelId<Item>(itemInfo.ItemId),
                                    IsHighQuality: false
                                )
                            }
                        )
                    }
                );
        }
    }

    // Unfortunately the source generator breaks on `WellFedOrMedicated` so we have to implement it manually
    public virtual bool Equals(PlayerStatusCondition? other) {
        return
            !ReferenceEquals(other, null) && EqualityContract == other.EqualityContract
            && global::Generator.Equals.UnorderedEqualityComparer<WellFedOrMedicated>.Default.Equals(this.Statuses!, other.Statuses!);
    }

    public override int GetHashCode() {
        var hashCode = new global::System.HashCode();

        hashCode.Add(this.EqualityContract);
        hashCode.Add(
            this.Statuses!,
            global::Generator.Equals.UnorderedEqualityComparer<WellFedOrMedicated>.Default
        );

        return hashCode.ToHashCode();
    }
}

[Equatable(Explicit = true)]
public partial record class WellFedOrMedicated(
    [property: DefaultEquality] ItemInfo.Type ConsumableType,
    [property: DefaultEquality] ExcelId<Item> Item,
    [property: DefaultEquality] bool IsHighQuality
) {
    public string NarrowName {
        get {
            var effectName = ConsumableType == ItemInfo.Type.FOOD ? "Well Fed" : "Medicated";
            if (Item.Id == 0) { return effectName; }

            var hqString = IsHighQuality ? " \uE03C" : "";
            return $"{Item.Name()}{hqString} ({Item.Id})";
        }
    }

    public string ValueName {
        get {
            var effectName = ConsumableType == ItemInfo.Type.FOOD ? "Well Fed" : "Medicated";
            if (Item.Id == 0) { return effectName; }

            var hqString = IsHighQuality ? " \uE03C" : "";
            return $"{effectName} - {Item.Name()}{hqString} ({Item.Id})";
        }
    }

    public bool SatisfiedBy(WellFedOrMedicated other) {
        // If Item is 0, assume it is satisfied if our type matches
        if (Item.Id == 0) { return this.ConsumableType == other.ConsumableType; }

        // Otherwise check if our current food is our expected food
        return this.ConsumableType == other.ConsumableType
            && this.Item.Id == other.Item.Id
            && this.IsHighQuality == other.IsHighQuality;
    }
}
