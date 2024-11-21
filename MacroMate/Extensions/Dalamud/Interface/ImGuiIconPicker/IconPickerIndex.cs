using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Utility;
using Lumina.Excel;
using Lumina.Excel.Sheets;
using Lumina.Text.ReadOnly;
using MacroMate.Extensions.Dotnet;
using FFXIVAction = Lumina.Excel.Sheets.Action;

namespace MacroMate.Extensions.Dalamud.Interface.ImGuiIconPicker;

/// Stores an index of most "Named" icons.
///
/// The sheets for this index are hand-selected so it may not be exhaustive,
/// but it covers most of the game icons.
public class IconPickerIndex {
    /// The full range of icons that we will try and explore.
    private List<int> iconRange = Enumerable.Range(0, 200000).ToList();

    /// The range of icons within iconRange that should be ignored (because they cause exceptions).
    private readonly HashSet<int> iconRangeNullValues = Enumerable.Range(170000, 9999).ToHashSet();

    private SortedList<uint, IconInfo> iconInfos = new();
    private SortedList<IconInfoCategory, IconInfoCategoryGroup> iconInfoGroupForCategory = new(IconInfoCategory.NameComparer);

    public enum IndexState {
        UNINDEXED,
        INDEXING,
        INDEXED
    };

    public IndexState State { get; private set; } = IndexState.UNINDEXED;

    public IconPickerIndex() {}

    public void StartIndexing(System.Action onFinish) {
        if (State != IndexState.UNINDEXED) { return; }

        Task.Run(() => {
            try {
                State = IndexState.INDEXING;
                ComputeValidIconIds();
                ApplyHardcodedCategories();
                ApplyGamedataIconInfo();
                IndexIcons();
                State = IndexState.INDEXED;
                onFinish();
            } catch (Exception ex) {
                Env.PluginLog.Error($"Failed to index icons\n{ex}");
            }

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

            if (!Env.TextureProvider.TryGetIconPath((uint)iconId, out var _)) {
                continue;
            }

            var iconInfo = new IconInfo {
                IconId = (uint)iconId
            };
            iconInfos.Add(iconInfo.IconId, iconInfo);
        }
    }

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
        ApplyIconCategory(4_000..4_400, "Mounts");
        ApplyIconCategory(4_400..5_100, "Minions");
        ApplyIconCategory(5_100..8_000, "Actions", "Traits");
        ApplyIconCategory(8_000..9_000, "Actions", "Fashion");
        ApplyIconCategory(9_000..10_000, "Actions", "PvP");
        ApplyIconCategory(10_000..20_000, "Statuses");
        ApplyIconCategory(20_000..30_000, "Items", "General");
        ApplyIconCategory(30_000..50_000, "Items", "Equipment");
        ApplyIconCategory(50_000..54_000, "Items", "Housing");
        ApplyIconCategory(54_000..54_400, "Items", "Equipment");
        ApplyIconCategory(54_400..58_000, "Items", "Equipment");
        ApplyIconCategory(58_000..59_000, "Items", "Fashion");
        ApplyIconCategory(59_000..59_400, "Mounts");
        ApplyIconCategory(59_400..60_000, "Minions");
        ApplyIconCategory(60_000..60_100, "System", "UI");
        ApplyIconCategory(60_100..60_200, "Icons", "Item Categories");
        ApplyIconCategory(60_200..60_300, "System", "Weather");
        ApplyIconCategory(60_300..60_650, "System", "Map Markers");
        ApplyIconCategory(60_650..60_720, "System", "UI");
        ApplyIconCategory(60_720..60_890, "System", "Map Markers");
        ApplyIconCategory(60_890..60_900, "Textures", "Art");
        ApplyIconCategory(60_900..61_000, "System", "UI");
        ApplyIconCategory(61_000..61_100, "Textures", "Splash Logos");
        ApplyIconCategory(61_100..61_200, "Actions", "Event");
        ApplyIconCategory(61_200..61_250, "System", "Markers");
        ApplyIconCategory(61_250..61_290, "Actions", "Duties/Trials");
        ApplyIconCategory(61_290..61_390, "System", "Markers");
        ApplyIconCategory(61_390..61_470, "System", "UI");
        ApplyIconCategory(61_470..61_600, "Icons", "Nameplate");
        ApplyIconCategory(61_600..61_635, "Icons", "Guardian");
        ApplyIconCategory(61_635..61_660, "Icons", "PvP");
        ApplyIconCategory(61_660..61_680, "Spoilers", "Custom Deliveries");
        ApplyIconCategory(61_680..61_690, "Textures", "Art");
        ApplyIconCategory(61_690..61_700, "Icons", "Grand Company");
        ApplyIconCategory(61_700..61_720, "Icons", "Nameplate");
        ApplyIconCategory(61_720..61_750, "Icons", "Quests");
        ApplyIconCategory(61_750..61_900, "Icons", "Playstyle");
        ApplyIconCategory(61_900..61_920, "Icons", "Tribes");
        ApplyIconCategory(61_920..61_960, "Icons", "Quests");
        ApplyIconCategory(61_960..62_000, "Icons", "Nameplate");
        ApplyIconCategory(62_000..62_600, "Icons", "Class/Job");
        ApplyIconCategory(62_600..62_620, "System", "Map Markers");
        ApplyIconCategory(62_620..62_800, "Textures", "World Map");
        ApplyIconCategory(62_800..62_900, "Icons", "Class/Job");
        ApplyIconCategory(62_900..63_000, "Icons", "Achievements");
        ApplyIconCategory(63_000..63_150, "Icons", "Hunt Log");
        ApplyIconCategory(63_150..63_170, "Textures", "PvP");
        ApplyIconCategory(63_170..63_180, "Textures", "Ocean Fishing");
        ApplyIconCategory(63_180..63_200, "Textures", "Submarine Maps");
        ApplyIconCategory(63_200..63_900, "Textures", "Zone Maps");
        ApplyIconCategory(63_900..64_000, "System", "Map Markers");
        ApplyIconCategory(64_000..64_200, "Actions", "Emotes");
        ApplyIconCategory(64_200..64_325, "Actions", "Free Company");
        ApplyIconCategory(64_325..64_500, "Actions", "Emotes");
        ApplyIconCategory(64_500..64_600, "Icons", "Group Pose"); // Stamps 1
        ApplyIconCategory(64_600..64_800, "Actions", "Eureka");
        ApplyIconCategory(64_800..65_000, "Actions", "NPC");
        ApplyIconCategory(65_000..65_900, "Icons", "Currencies");
        ApplyIconCategory(65_900..66_000, "Icons", "Ocean Fishing");
        ApplyIconCategory(66_000..66_400, "Icons", "Macros");
        ApplyIconCategory(66_400..66_500, "Icons", "Tags");
        ApplyIconCategory(66_500..67_000, "Textures", "Gardening Log");
        ApplyIconCategory(67_000..68_000, "Actions", "Fashion");
        ApplyIconCategory(68_000..68_400, "Mounts", "Log");
        ApplyIconCategory(68_400..69_000, "Minions", "Log");
        ApplyIconCategory(69_000..70_000, "Textures", "Footprints"); // Mount/Minion
        ApplyIconCategory(70_000..70_120, "Actions", "Chocobo Racing");
        ApplyIconCategory(70_120..70_200, "Icons", "Island Sanctuary");
        ApplyIconCategory(70_200..71_000, "Textures", "DoH/DoL Logs");
        ApplyIconCategory(71_000..71_450, "Icons", "Quests");
        ApplyIconCategory(71_450..72_000, "Spoilers", "Credits");
        ApplyIconCategory(72_000..72_500, "System", "BLU UI");
        ApplyIconCategory(72_500..72_620, "System", "Bozja UI");
        ApplyIconCategory(72_620..76_000, "Spoilers", "NPC Portraits");
        ApplyIconCategory(76_000..76_170, "System", "Mahjong");
        ApplyIconCategory(76_170..76_200, "Icons", "Group Pose");
        ApplyIconCategory(76_200..76_300, "Textures", "Fan Festival");
        ApplyIconCategory(76_300..78_000, "Icons", "Group Pose");
        ApplyIconCategory(78_000..80_000, "Textures", "Fishing Log");
        ApplyIconCategory(80_000..80_200, "Icons", "Quests"); // Quest Log
        ApplyIconCategory(80_200..80_730, "Textures", "Notebooks");
        ApplyIconCategory(80_730..81_000, "Textures", "Relic Log");
        ApplyIconCategory(81_000..82_000, "Textures", "Notebooks");
        ApplyIconCategory(82_000..82_011, "Icons", "Negotiation");
        ApplyIconCategory(82_011..82_020, "Textures", "Art");
        ApplyIconCategory(82_020..82_040, "Icons", "Orchestration");
        ApplyIconCategory(82_040..82_050, "Icons", "Island Sanctuary");
        ApplyIconCategory(82_050..82_080, "Spoilers", "Fall Guys");
        ApplyIconCategory(82_080..83_000, "Icons"); // This used to be triple triad, not anymore?
        ApplyIconCategory(83_000..84_000, "Icons", "Grand Company");
        ApplyIconCategory(84_000..85_000, "Textures", "Hunts");
        ApplyIconCategory(85_000..90_000, "Textures", "UI");
        ApplyIconCategory(90_000..100_000, "Icons", "Free Company");
        ApplyIconCategory(100_000..114_000, "Spoilers", "Quest Images");
        ApplyIconCategory(114_100..120_000, "Spoilers", "New Game+");
        ApplyIconCategory(120_000..130_000, "Spoilers", "Popup Texts");
        ApplyIconCategory(130_000..142_000, "Aesthetics");
        ApplyIconCategory(142_000..150_000, "Spoilers", "Japanese Popup Texts");
        ApplyIconCategory(150_000..170_000, "Textures", "Tutorials");
        // 170_000..180_000 -- blank placeholder files
        ApplyIconCategory(180_000..180_060, "Textures", "Stamps/Chocobo Racing");
        ApplyIconCategory(180_060..180_100, "Spoilers", "Fall Guys");
        ApplyIconCategory(180_100..181_000, "Textures", "Tutorials");
        ApplyIconCategory(181_000..181_500, "Spoilers", "Boss Titles");
        ApplyIconCategory(181_500..200_000, "Spoilers", "Adventurer Plate");
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
        // Ignored: Addon / AddonTransient
        // Ignored: "AnimaWeaponIcon"?
        // Ignored: "CharaMakeCustomize"
        // Ignored: "EventCustomIconType"
        // Ignored: "Frontline03"

        ApplyIconNamesFrom<FFXIVAction>(
            (row) => row.Icon,
            (row) => new[] { row.Name, row.ClassJob.ValueNullable?.Name, row.ActionCategory.ValueNullable?.Name }
        );

        ApplyIconNamesFrom<Adventure>(
            (row) => (uint)row.IconList,
            (row) => new[] { row.Name, row.PlaceName.ValueNullable?.Name }
        );

        ApplyIconNamesFrom<AOZContentBriefingBNpc>(
            (row) => new[] { row.TargetSmall, row.TargetLarge },
            (row) => new[] { row.BNpcName.ValueNullable?.Singular }.WithoutNull()
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
            (row) => new[] { row.Category.ValueNullable?.Text }
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
            (row) => new[] { row.Name, row.ClassJob.ValueNullable?.Name, row.ClassJobCategory.ValueNullable?.Name }
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
            (row) => new[] { row.Name, row.EmoteCategory.ValueNullable?.Name }
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
            (row) => new[] { row.Item.ValueNullable?.Singular }
        );

        ApplyIconNamesFrom<GatheringType>(
            (row) => new[] { (uint)row.IconMain, (uint)row.IconOff },
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<GcArmyCaptureTactics>(
            (row) => row.Icon,
            (row) => new[] { row.Name.ValueNullable?.Name }
        );

        ApplyIconNamesFrom<GeneralAction>(
            (row) => new[] { (uint)row.Icon },
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<GroupPoseStamp>(
            (row) => (uint)row.StampIcon,
            (row) => new[] { row.Name, row.Category.ValueNullable?.Name }
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

        // TODO: Broken in 7.1, fix later
        // ApplyIconNamesFrom<ManeuversArmor>(
        //     (row) => row.NeutralMapIcon,
        //     (row) => new[] { row.Unknown10 }
        // );

        // TODO: Broken in 7.1, fix later
        // ApplyIconNamesFrom<MapMarker>(
        //     (row) => (uint)row.Icon,
        //     (row) => new[] { row.PlaceNameSubtext.ValueNullable?.Name }
        // );

        ApplyIconNamesFrom<MapSymbol>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.PlaceName.ValueNullable?.Name }
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
            (row) => row.Reward.Select(item => item.ValueNullable?.Name)
        );

        // TODO: Broken in 7.1, fix later
        // ApplyIconNamesFrom<MJIBuilding>(
        //     (row) => row.Icon,
        //     (row) => new[] { row.Name.ValueNullable?.Text }
        // );

        ApplyIconNamesFrom<MJIGatheringObject>(
            (row) => (uint)row.MapIcon,
            (row) => new[] { row.Name.ValueNullable?.Singular }
        );

        ApplyIconNamesFrom<MJIHudMode>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        ApplyIconNamesFrom<MobHuntTarget>(
            (row) => row.Icon,
            (row) => new[] { row.Name.ValueNullable?.Singular }
        );

        ApplyIconNamesFrom<MonsterNoteTarget>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.BNpcName.ValueNullable?.Singular }
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
            (row) => new[] { row.ContentFinderCondition.ValueNullable?.Name }
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
            (row) => new[] { row.ItemAtma.ValueNullable?.Name, row.ItemAnimus.ValueNullable?.Name }
        );

        ApplyIconNamesFrom<Relic3>(
            (row) => (uint)row.Icon,
            (row) => new[] {
                row.ItemAnimus.ValueNullable?.Name,
                row.ItemScroll.ValueNullable?.Name,
                row.ItemNovus.ValueNullable?.Name
            }.WithoutNull()
        );

        ApplyIconNamesFrom<SatisfactionNpc>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Npc.ValueNullable?.Singular }
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
            (row) => new[] { row.ItemName.ValueNullable?.Name, row.KeyItemName.ValueNullable?.Name }
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
            (row) => new[] { row.Text.ValueNullable?.Description }
        );
    }

    private void IndexWeird() {
    }

    private void ApplyIconNamesFrom<Row>(
        Func<Row, IEnumerable<uint>> icons,
        Func<Row, IEnumerable<ReadOnlySeString>> names
    ) where Row : struct, IExcelRow<Row> {
        var excelSheet = Env.DataManager.GetExcelSheet<Row>();
        if (excelSheet == null) { return; }

        foreach (var row in excelSheet) {
            var usefulNames = names(row)
                .WithoutNull()
                .Select(name => name.ExtractText())
                .Where(name => !name.IsNullOrEmpty())
                .ToList();
            if (usefulNames.Count == 0) { continue; }

            var usefulIcons = icons(row)
                .Where(icon => icon != 0);

            foreach (var usefulIcon in usefulIcons) {
                // If no icon info exists then this isn't a valid id
                var existingIconInfo = iconInfos.GetValueOrDefault((uint)usefulIcon);
                if (existingIconInfo == null) { continue; }

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
        Func<Row, IEnumerable<ReadOnlySeString?>> names
    ) where Row : struct, IExcelRow<Row> {
        ApplyIconNamesFrom<Row>(
            (row) => new[] { icon(row) },
            (row) => names(row).WithoutNull()
        );
    }

    private void ApplyIconNamesFrom<Row>(
        Func<Row, uint> icon,
        Func<Row, IEnumerable<ReadOnlySeString>> names
    ) where Row : struct, IExcelRow<Row> {
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
