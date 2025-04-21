using System.Collections.Generic;
using Lumina.Excel.Sheets;

namespace MacroMate.Extensions.Dalamud;

/// <summary>
/// Index to help look up item information
/// </summary>
public class ItemIndex {
    private Dictionary<uint, ItemInfo> ItemFoodIdToItemInfo = new();

    public ItemInfo? FindFoodItemInfo(uint itemFoodId) {
        RefreshIndex();

        if (ItemFoodIdToItemInfo.TryGetValue(itemFoodId, out var itemInfo)) {
            return itemInfo;
        } else {
            return null;
        }
    }

    private void RefreshIndex() {
        if (ItemFoodIdToItemInfo.Count > 0) { return; }

        foreach (var item in Env.DataManager.GetExcelSheet<Item>()) {
            var isFood = item.ItemUICategory.RowId == 46;
            var isMedicine = item.ItemUICategory.RowId == 44;
            if (!isFood && !isMedicine) { continue; }

            var itemAction = item.ItemAction.ValueNullable;
            if (!itemAction.HasValue) { continue; }

            if (itemAction.Value.Data.Count < 2) { continue; }
            var itemFoodId = itemAction.Value.Data[1];

            var itemType = isFood ? ItemInfo.Type.FOOD : ItemInfo.Type.MEDICINE;

            ItemFoodIdToItemInfo[itemFoodId] = new ItemInfo() {
                ItemType = itemType,
                ItemId = item.RowId,
                IsHighQuality = false
            };

            // HQ items are the original id + 10000
            ItemFoodIdToItemInfo[itemFoodId + 10000u] = new ItemInfo() {
                ItemType = itemType,
                ItemId = item.RowId,
                IsHighQuality = true
            };
        }
    }
}

public class ItemInfo {
    public required Type ItemType { get; init; }
    public required uint ItemId { get; init; }
    public bool IsHighQuality { get; init; } = false;

    public enum Type { FOOD, MEDICINE }
}
