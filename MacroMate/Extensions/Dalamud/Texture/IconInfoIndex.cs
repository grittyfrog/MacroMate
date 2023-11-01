using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Lumina.Text;
using MacroMate.Extensions.Dotnet;
using MacroMate.Extensions.Lumina;
using FFXIVAction = Lumina.Excel.GeneratedSheets.Action;

namespace MacroMate.Extensions.Dalamud.Texture;

/// Stores an index of most "Named" icons.
///
/// The sheets for this index are hand-selected so it may not be exhaustive,
/// but it covers most of the game icons.
public class IconInfoIndex {
    /// The full range of icons that we will try and explore.
    private List<int> iconRange = Enumerable.Range(0, 200000).ToList();

    /// The range of icons within iconRange that should be ignored (because they cause exceptions).
    private readonly HashSet<int> iconRangeNullValues = Enumerable.Range(170000, 9999).ToHashSet();

    private SortedList<uint, IconInfo> iconInfos = new();
    private SortedList<IconInfoCategory, IconInfoCategoryGroup> iconInfoGroupForCategory = new(IconInfoCategory.NameComparer);

    // private SortedSet<IconInfoCategoryGroup> iconInfoCategoryGroups = new(IconInfoCategoryGroup.CompareByCategory);
    // private ConcurrentDictionary<IconInfoCategory, SortedSet<IconInfo>> iconInfoByCategory = new();

    public enum IndexState {
        UNINDEXED,
        INDEXING,
        INDEXED
    };

    public IndexState State { get; private set; } = IndexState.UNINDEXED;

    public IconInfoIndex() {}

    public void StartIndexing(System.Action onFinish) {
        if (State != IndexState.UNINDEXED) { return; }

        Task.Run(() => {
            State = IndexState.INDEXING;
            ComputeValidIconIds();
            ApplyHardcodedCategories();
            ApplyGamedataIconInfo();
            IndexIcons();
            State = IndexState.INDEXED;
            onFinish();
        });
    }

    public List<IconInfoCategoryGroup> CategoryRoots() => iconInfoGroupForCategory.Values.ToList();

    public List<IconInfo> All(IconInfoCategory? category = null) {
        if (State != IndexState.INDEXED) { return new(); }
        if (category == null) { return iconInfos.Values.ToList(); }

        return iconInfoGroupForCategory
            .GetValueOrDefault(category)
            ?.GetIconsForCategory(category)
            ?.ToList()
            ?? new();
    }

    public List<IconInfo> NameSearch(string needle, IconInfoCategory? category = null) {
        if (State != IndexState.INDEXED) { return new(); }
        if (needle.Length < 2)  { return new(); }

        var searchNeedle = needle.ToLowerInvariant();

        var searchHaystack = All(category);

        IEnumerable<(IconInfo, int)> results = searchHaystack
            .Select<IconInfo, (IconInfo, int)?>(icon => {
                if (icon.IconId.ToString() == searchNeedle) { return (icon, 0); }
                foreach (var searchName in icon.SearchNames) {
                    if (searchName.ToLowerInvariant() == searchNeedle) { return (icon, 1); }
                    if (searchName.Split(" ").Any(word => word == searchNeedle)) { return (icon, 2); }
                    if (searchName.Contains(searchNeedle)) { return (icon, 3); }
                }

                return null;
            })
            .OfType<(IconInfo, int)>();

        return results
            .OrderBy(iconAndScore => iconAndScore.Item2)
            .Select(iconAndScore => iconAndScore.Item1)
            .ToList();
    }

    private void AddOrMergeIconInfo(IconInfo iconInfo) {
        IconInfo? existingIconInfo;
        if (iconInfos.TryGetValue(iconInfo.IconId, out existingIconInfo)) {
            existingIconInfo.AddNames(iconInfo.Names);
            existingIconInfo.AddCategories(iconInfo.Categories);
        } else {
            existingIconInfo = iconInfo;
            this.iconInfos.Add(iconInfo.IconId, iconInfo);
        }
    }

    private void ComputeValidIconIds() {
        foreach (var iconId in iconRange) {
            if (iconRangeNullValues.Contains(iconId)) { continue; }

            var iconPath = Env.TextureProvider.GetIconPath((uint)iconId);
            if (iconPath.IsNullOrEmpty()) { continue; }
            if (!Env.DataManager.FileExists(iconPath)) { continue; }

            var iconInfo = new IconInfo {
                IconId = (uint)iconId
            };
            iconInfos.Add(iconInfo.IconId, iconInfo);
        }
    }

    // iiCG[Action] = ActionGroup()
    // iiCG[Action (Weaponskill)] = ActionGroup()
    //
    private void IndexIcons() {
        foreach (var iconInfo in iconInfos.Values) {
            foreach (var category in iconInfo.Categories) {
                // The "Top Level" category and all subcategories should point to the same group.
                var categoryGroup = iconInfoGroupForCategory.GetOrAdd(
                    category.TopLevel(),
                    () => new IconInfoCategoryGroup { Category = category.TopLevel() }
                );
                iconInfoGroupForCategory.TryAdd(category, categoryGroup);
                categoryGroup.AddIconForCategory(category, iconInfo);
            }
        }
    }

    /// Assumes that `IndexIconIds` has already been run
    private void ApplyHardcodedCategories() {
        // First add the categories to the correct icons.
        ApplyIconCategory(1..100, "System");
        ApplyIconCategory(100..4_000, "Actions", "Class/Job");
        ApplyIconCategory(4_000..4_400, "Items", "Mounts");
        ApplyIconCategory(4_400..5_100, "Items", "Minions");
        ApplyIconCategory(62_000..62_600, "Actions", "Class/Job");

        // Now go through all our icons and index the categories
    }

    private void ApplyIconCategory(Range range, string category, string? subcategory = null) {
        foreach (var iconId in range.Enumerate()) {
            var iconInfo = iconInfos.GetValueOrDefault((uint)iconId);
            if (iconInfo == null) { continue; }

            var categorisedIconInfo = new IconInfo {
                IconId = (uint)iconId,
            };
            categorisedIconInfo.AddCategory(new IconInfoCategory(category, subcategory));
            AddOrMergeIconInfo(categorisedIconInfo);
        }
    }

    private void ApplyGamedataIconInfo() {
        ApplyIconNamesFrom<FFXIVAction>(
            (row) => row.Icon,
            (row) => new[] { row.Name, row.ClassJob.Value?.Name, row.ActionCategory.Value?.Name }
        );

        ApplyIconNamesFrom<Adventure>(
            (row) => (uint)row.IconList,
            (row) => new[] { row.Name, row.PlaceName.Value?.Name }
        );

        ApplyIconNamesFrom<AOZContentBriefingBNpc>(
            (row) => new[] { row.TargetSmall, row.TargetLarge },
            (row) => new[] { row.BNpcName.Value?.Singular }
        );

        ApplyIconNamesFrom<BeastTribe>(
            (row) => new[] { row.IconReputation, row.Icon },
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<BuddyAction>(
            (row) => new[] { (uint)row.Icon, (uint)row.IconStatus },
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<BuddyEquip>(
            (row) => new uint[] { row.IconHead, row.IconBody, row.IconLegs },
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<CabinetCategory>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Category.Value?.Text }
        );

        ApplyIconNamesFrom<CharaCardPlayStyle>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<ChocoboRaceAbility>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<ChocoboRaceItem>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<CircleActivity>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<Companion>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Singular }
        );

        ApplyIconNamesFrom<CompanyAction>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name, row.Description }
        );

        ApplyIconNamesFrom<ContentsNote>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name, row.Description }
        );

        ApplyIconNamesFrom<ContentType>(
            (row) => new uint[] { row.Icon, row.IconDutyFinder },
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<CraftAction>(
            (row) => row.Icon,
            (row) => new[] { row.Name, row.ClassJob.Value?.Name, row.ClassJobCategory.Value?.Name }
        );

        ApplyIconNamesFrom<DeepDungeonEquipment>(
            (row) => row.Icon,
            (row) => new[] { row.Singular }
        );

        ApplyIconNamesFrom<DeepDungeonFloorEffectUI>(
            (row) => row.Icon,
            (row) => new[] { row.Name, row.Description }
        );

        ApplyIconNamesFrom<DeepDungeonItem>(
            (row) => row.Icon,
            (row) => new[] { row.Singular, row.Tooltip }
        );

        ApplyIconNamesFrom<DeepDungeonMagicStone>(
            (row) => row.Icon,
            (row) => new[] { row.Singular, row.Tooltip }
        );

        ApplyIconNamesFrom<Emote>(
            (row) => row.Icon,
            (row) => new[] { row.Name, row.EmoteCategory.Value?.Name }
        );

        ApplyIconNamesFrom<EventAction>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<EventItem>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<FCRights>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<GardeningSeed>(
            (row) => row.Icon,
            (row) => new[] { row.Item.Value?.Singular }
        );

        ApplyIconNamesFrom<GatheringType>(
            (row) => new[] { (uint)row.IconMain, (uint)row.IconOff },
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<GcArmyCaptureTactics>(
            (row) => row.Icon,
            (row) => new[] { row.Name.Value?.Name }
        );

        ApplyIconNamesFrom<GeneralAction>(
            (row) => new[] { (uint)row.Icon },
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<GroupPoseStamp>(
            (row) => new[] { (uint)row.StampIcon },
            (row) => new[] { row.Name, row.Category.Value?.Name }
        );

        ApplyIconNamesFrom<GuardianDeity>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<HousingAppeal>(
            (row) => new[] { row.Icon },
            (row) => new[] { row.Tag }
        );

        ApplyIconNamesFrom<IKDContentBonus>(
            (row) => new[] { row.Image },
            (row) => new[] { row.Objective }
        );

        ApplyIconNamesFrom<Item>(
            (row) => new[] { (uint)row.Icon },
            (row) => new[] { row.Name, row.Description }
        );

        ApplyIconNamesFrom<ItemSearchCategory>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<ItemUICategory>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<MainCommand>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name, row.Description }
        );

        ApplyIconNamesFrom<ManeuversArmor>(
            (row) => row.Icon,
            (row) => new[] { row.Unknown10 }
        );

        ApplyIconNamesFrom<MapMarker>(
            (row) => row.Icon,
            (row) => new[] { row.PlaceNameSubtext.Value?.Name }
        );

        ApplyIconNamesFrom<MapSymbol>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.PlaceName.Value?.Name }
        );

        ApplyIconNamesFrom<Marker>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<McGuffinUIData>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        // TODO: This doesn't have the actual animal names, can we get them?
        ApplyIconNamesFrom<MJIAnimals>(
            (row) => (uint)row.Icon,
            (row) => row.Reward.Select(item => item.Value?.Name)
        );

        ApplyIconNamesFrom<MJIBuilding>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name.Value?.Text }
        );

        ApplyIconNamesFrom<MJIGatheringObject>(
            (row) => (uint)row.MapIcon,
            (row) => new[] { row.Name.Value?.Singular }
        );

        ApplyIconNamesFrom<MJIHudMode>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<MobHuntTarget>(
            (row) => row.Icon,
            (row) => new[] { row.Name.Value?.Singular }
        );

        ApplyIconNamesFrom<MonsterNoteTarget>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.BNpcName.Value?.Singular }
        );

        ApplyIconNamesFrom<Mount>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Singular }
        );

        ApplyIconNamesFrom<OrchestrionCategory>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<Ornament>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Singular }
        );

        ApplyIconNamesFrom<PetAction>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<PublicContent>(
            (row) => (uint)row.MapIcon,
            (row) => new[] { row.ContentFinderCondition.Value?.Name }
        );

        ApplyIconNamesFrom<PvPSelectTrait>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Effect }
        );

        ApplyIconNamesFrom<QuestRedoChapterUITab>(
            (row) => new[] { row.Icon1, row.Icon2 },
            (row) => new[] { row.Text }
        );

        ApplyIconNamesFrom<QuestRewardOther>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<QuickChat>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.NameAction }
        );

        ApplyIconNamesFrom<Relic>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.ItemAtma.Value?.Name, row.ItemAnimus.Value?.Name }
        );

        ApplyIconNamesFrom<Relic3>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.ItemAnimus.Value?.Name, row.ItemScroll.Value?.Name, row.ItemNovus.Value?.Name }
        );

        ApplyIconNamesFrom<SatisfactionNpc>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Npc.Value?.Singular }
        );

        ApplyIconNamesFrom<Status>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<SubmarineMap>(
            (row) => (uint)row.Image,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<Town>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<Trait>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<TreasureHuntRank>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.ItemName.Value?.Name, row.KeyItemName.Value?.Name }
        );

        ApplyIconNamesFrom<VVDNotebookContents>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name, row.Description }
        );

        ApplyIconNamesFrom<Weather>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<WeeklyBingoOrderData>(
            (row) => row.Icon,
            (row) => new[] { row.Text.Value?.Description }
        );
    }

    private void IndexWeird() {
        // TODO: Addon / AddonTransient (parse the strings?)
        // TODO: "AnimaWeaponIcon"?
        // TODO: "CharaMakeCustomize" (maybe)
        // TODO: "EventCustomIconType"
        // TODO: "Frontline03" (maybe)
    }


    private void ApplyIconNamesFrom<Row>(
        Func<Row, IEnumerable<uint>> icons,
        Func<Row, IEnumerable<SeString?>> names
    ) where Row : ExcelRow {
        var excelSheet = Env.DataManager.GetExcelSheet<Row>();
        if (excelSheet == null) { return; }

        foreach (var row in excelSheet) {
            var usefulNames = names(row)
                .WithoutNull()
                .Select(name => name.Text())
                .Where(name => !name.IsNullOrEmpty())
                .ToList();
            if (usefulNames.Count == 0) { continue; }

            var usefulIcons = icons(row)
                .Where(icon => icon != 0);

            foreach (var usefulIcon in usefulIcons) {
                var iconInfo = new IconInfo {
                    IconId = usefulIcon!,
                    Names = usefulNames
                };

                AddOrMergeIconInfo(iconInfo);
            }
        }
    }

    private void ApplyIconNamesFrom<Row>(
        Func<Row, uint> icon,
        Func<Row, IEnumerable<SeString?>> names
    ) where Row : ExcelRow {
        ApplyIconNamesFrom((row) => new[] { icon(row) }, names);
    }
}

public class IconInfo {
    public required uint IconId { get; init; }

    /// The names/descriptions that refer to this IconId.
    ///
    /// By convention this must always have at least one element, and the names
    /// should be ordered by "specificity", with the first name being the "best"
    private List<string> _names = new();
    public List<string> Names {
        get { return _names; }
        init {
            _names = value;
            SearchNames = value.Select(name => name.ToLowerInvariant()).ToList();
        }
    }

    /// Same as Names, but all lowercase
    public List<string> SearchNames { get; private set; } = new();

    /// The Categories that this Icon belongs to.
    public List<IconInfoCategory> Categories { get; private set; } = new();

    public void AddNames(IEnumerable<string> names) {
        foreach (var name in names) {
            var searchName = name.ToLowerInvariant();
            if (SearchNames.Contains(searchName)) { continue; }

            Names.Add(name);
            SearchNames.Add(searchName);
        }
    }

    public void AddCategory(IconInfoCategory category) {
        if (Categories.Contains(category)) { return; }
        Categories.Add(category);

        var topLevel = category.TopLevel();
        if (topLevel != category) {
            if (Categories.Contains(topLevel)) { return; }
            Categories.Add(topLevel);
        }
    }

    public void AddCategories(IEnumerable<IconInfoCategory> categories) {
        foreach (var category in categories) {
            AddCategory(category);
        }
    }

    public static Comparer<IconInfo> CompareById = Comparer<IconInfo>.Create((a, b) => a.IconId.CompareTo(b.IconId));
}

public record class IconInfoCategory(
    string Name,
    string? SubcategoryName = null
) {
    public IconInfoCategory TopLevel() => new IconInfoCategory(Name, SubcategoryName: null);

    public static Comparer<IconInfoCategory> NameComparer => ComparerExt.Compose(
        Comparer<IconInfoCategory>.Create((a, b) => a.Name.CompareTo(b.Name)),
        Comparer<IconInfoCategory>.Create((a, b) => a.SubcategoryName?.CompareTo(b.SubcategoryName) ?? 0)
    );

    public override string ToString() {
        if (SubcategoryName != null) {
            return $"{Name} ({SubcategoryName})";
        } else {
            return Name;
        }
    }
}

/**
 * <summary>Holds a IconInfo category and all subcategories</summary>
 */
public class IconInfoCategoryGroup {
    public required IconInfoCategory Category { get; set; }
    public SortedSet<IconInfo> IconInfos { get; private set; } = new(IconInfo.CompareById);
    public SortedList<IconInfoCategory, SortedSet<IconInfo>> SubcategoryIconInfos = new(IconInfoCategory.NameComparer);

    public void AddIconForCategory(IconInfoCategory category, IconInfo iconInfo) {
        // Icons are always added to the parent category
        IconInfos.Add(iconInfo);

        // If we have a subcategory, add to it as well
        if (category.SubcategoryName != null) {
            var subcategoryList = SubcategoryIconInfos.GetOrAdd(category, () => new(IconInfo.CompareById));
            subcategoryList.Add(iconInfo);
        }
    }

    public SortedSet<IconInfo> GetIconsForCategory(IconInfoCategory category) {
        if (Category == category) { return IconInfos; }
        foreach (var (subcategory, subcategoryIconInfos) in SubcategoryIconInfos) {
            if (subcategory == category) { return subcategoryIconInfos; }
        }
        return new();
    }

    public static Comparer<IconInfoCategoryGroup> CompareByCategory =
        Comparer<IconInfoCategoryGroup>.Create((a, b) => IconInfoCategory.NameComparer.Compare(a.Category, b.Category));
}
