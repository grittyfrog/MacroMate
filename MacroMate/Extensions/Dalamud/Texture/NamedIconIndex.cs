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
public class NamedIconIndex {
    /// The full range of icons that we will try and explore.
    private List<int> iconRange = Enumerable.Range(0, 200000).ToList();

    /// The range of icons within iconRange that should be ignored (because they cause exceptions).
    private readonly HashSet<int> iconRangeNullValues = Enumerable.Range(170000, 9999).ToHashSet();

    private List<IconInfo> iconInfo = new();

    private ConcurrentDictionary<uint, IconInfo> iconIdIndex = new();
    private ConcurrentDictionary<string, List<IconInfo>> iconCategoryIndex = new();

    public enum IndexState {
        UNINDEXED,
        INDEXING,
        INDEXED
    };

    public IndexState State { get; private set; } = IndexState.UNINDEXED;

    public NamedIconIndex() {}

    public void StartIndexing(System.Action onFinish) {
        if (State != IndexState.UNINDEXED) { return; }

        Task.Run(() => {
            State = IndexState.INDEXING;
            IndexNamedIcons();
            State = IndexState.INDEXED;
            onFinish();
        });
    }

    public List<string> AllCategories() => iconCategoryIndex.Keys.ToList();

    public List<IconInfo> NameSearch(string needle) {
        if (State != IndexState.INDEXED) { return new(); }
        if (needle.Length < 2)  { return new(); }

        IEnumerable<(IconInfo, int)> results = iconInfo
            .Select<IconInfo, (IconInfo, int)?>(icon => {
                if (icon.IconId.ToString() == needle) { return (icon, 0); }
                foreach (var searchName in icon.SearchNames) {
                    if (searchName == needle) { return (icon, 1); }
                    if (searchName.Split(" ").Any(word => word == needle)) { return (icon, 2); }
                    if (searchName.Contains(needle)) { return (icon, 3); }
                }

                return null;
            })
            .OfType<(IconInfo, int)>();

        return results
            .OrderBy(iconAndScore => iconAndScore.Item2)
            .Select(iconAndScore => iconAndScore.Item1)
            .ToList();
    }

    private void IndexNamedIcons() {
        IndexIcons<FFXIVAction>(
            (row) => row.Icon,
            (row) => new[] { row.Name, row.ClassJob.Value?.Name, row.ActionCategory.Value?.Name }
        );

        IndexIcons<Adventure>(
            (row) => (uint)row.IconList,
            (row) => new[] { row.Name, row.PlaceName.Value?.Name }
        );

        IndexIcons<AOZContentBriefingBNpc>(
            (row) => new[] { row.TargetSmall, row.TargetLarge },
            (row) => new[] { row.BNpcName.Value?.Singular }
        );

        IndexIcons<BeastTribe>(
            (row) => new[] { row.IconReputation, row.Icon },
            (row) => new[] { row.Name }
        );

        IndexIcons<BuddyAction>(
            (row) => new[] { (uint)row.Icon, (uint)row.IconStatus },
            (row) => new[] { row.Name }
        );

        IndexIcons<BuddyEquip>(
            (row) => new uint[] { row.IconHead, row.IconBody, row.IconLegs },
            (row) => new[] { row.Name }
        );

        IndexIcons<CabinetCategory>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Category.Value?.Text }
        );

        IndexIcons<CharaCardPlayStyle>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<ChocoboRaceAbility>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<ChocoboRaceItem>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<CircleActivity>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<Companion>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Singular }
        );

        IndexIcons<CompanyAction>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name, row.Description }
        );

        IndexIcons<ContentsNote>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name, row.Description }
        );

        IndexIcons<ContentType>(
            (row) => new uint[] { row.Icon, row.IconDutyFinder },
            (row) => new[] { row.Name }
        );

        IndexIcons<CraftAction>(
            (row) => row.Icon,
            (row) => new[] { row.Name, row.ClassJob.Value?.Name, row.ClassJobCategory.Value?.Name }
        );

        IndexIcons<DeepDungeonEquipment>(
            (row) => row.Icon,
            (row) => new[] { row.Singular }
        );

        IndexIcons<DeepDungeonFloorEffectUI>(
            (row) => row.Icon,
            (row) => new[] { row.Name, row.Description }
        );

        IndexIcons<DeepDungeonItem>(
            (row) => row.Icon,
            (row) => new[] { row.Singular, row.Tooltip }
        );

        IndexIcons<DeepDungeonMagicStone>(
            (row) => row.Icon,
            (row) => new[] { row.Singular, row.Tooltip }
        );

        IndexIcons<Emote>(
            (row) => row.Icon,
            (row) => new[] { row.Name, row.EmoteCategory.Value?.Name }
        );

        IndexIcons<EventAction>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<EventItem>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<FCRights>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<GardeningSeed>(
            (row) => row.Icon,
            (row) => new[] { row.Item.Value?.Singular }
        );

        IndexIcons<GatheringType>(
            (row) => new[] { row.IconMain, row.IconOff },
            (row) => new[] { row.Name }
        );

        IndexIcons<GcArmyCaptureTactics>(
            (row) => row.Icon,
            (row) => new[] { row.Name.Value?.Name }
        );

        IndexIcons<GeneralAction>(
            (row) => new[] { row.Icon },
            (row) => new[] { row.Name }
        );

        IndexIcons<GroupPoseStamp>(
            (row) => new[] { row.StampIcon },
            (row) => new[] { row.Name, row.Category.Value?.Name }
        );

        IndexIcons<GuardianDeity>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<HousingAppeal>(
            (row) => new[] { row.Icon },
            (row) => new[] { row.Tag }
        );

        IndexIcons<IKDContentBonus>(
            (row) => new[] { row.Image },
            (row) => new[] { row.Objective }
        );

        IndexIcons<Item>(
            (row) => new[] { (uint)row.Icon },
            (row) => new[] { row.Name, row.Description }
        );

        IndexIcons<ItemSearchCategory>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<ItemUICategory>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<MainCommand>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name, row.Description }
        );

        IndexIcons<ManeuversArmor>(
            (row) => row.Icon,
            (row) => new[] { row.Unknown10 }
        );

        IndexIcons<MapMarker>(
            (row) => row.Icon,
            (row) => new[] { row.PlaceNameSubtext.Value?.Name }
        );

        IndexIcons<MapSymbol>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.PlaceName.Value?.Name }
        );

        IndexIcons<Marker>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<McGuffinUIData>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        // TODO: This doesn't have the actual animal names, can we get them?
        IndexIcons<MJIAnimals>(
            (row) => (uint)row.Icon,
            (row) => row.Reward.Select(item => item.Value?.Name)
        );

        IndexIcons<MJIBuilding>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name.Value?.Text }
        );

        IndexIcons<MJIGatheringObject>(
            (row) => (uint)row.MapIcon,
            (row) => new[] { row.Name.Value?.Singular }
        );

        IndexIcons<MJIHudMode>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<MobHuntTarget>(
            (row) => row.Icon,
            (row) => new[] { row.Name.Value?.Singular }
        );

        IndexIcons<MonsterNoteTarget>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.BNpcName.Value?.Singular }
        );

        IndexIcons<Mount>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Singular }
        );

        IndexIcons<OrchestrionCategory>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<Ornament>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Singular }
        );

        IndexIcons<PetAction>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<PublicContent>(
            (row) => (uint)row.MapIcon,
            (row) => new[] { row.ContentFinderCondition.Value?.Name }
        );

        IndexIcons<PvPSelectTrait>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Effect }
        );

        IndexIcons<QuestRedoChapterUITab>(
            (row) => new[] { row.Icon1, row.Icon2 },
            (row) => new[] { row.Text }
        );

        IndexIcons<QuestRewardOther>(
            (row) => row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<QuickChat>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.NameAction }
        );

        IndexIcons<Relic>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.ItemAtma.Value?.Name, row.ItemAnimus.Value?.Name }
        );

        IndexIcons<Relic3>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.ItemAnimus.Value?.Name, row.ItemScroll.Value?.Name, row.ItemNovus.Value?.Name }
        );

        IndexIcons<SatisfactionNpc>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Npc.Value?.Singular }
        );

        IndexIcons<Status>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<SubmarineMap>(
            (row) => (uint)row.Image,
            (row) => new[] { row.Name }
        );

        IndexIcons<Town>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<Trait>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<TreasureHuntRank>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.ItemName.Value?.Name, row.KeyItemName.Value?.Name }
        );

        IndexIcons<VVDNotebookContents>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name, row.Description }
        );

        IndexIcons<Weather>(
            (row) => (uint)row.Icon,
            (row) => new[] { row.Name }
        );

        IndexIcons<WeeklyBingoOrderData>(
            (row) => row.Icon,
            (row) => new[] { row.Text.Value?.Description }
        );
    }

    private void IndexWeird() {
        // TODO: "AnimaWeaponIcon"?
        // TODO: "CharaMakeCustomize" (maybe)
        // TODO: "EventCustomIconType"
        // TODO: "Frontline03" (maybe)
    }


    private void IndexIcons<Row>(
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

                if (iconIdIndex.ContainsKey(iconInfo.IconId)) {
                    var existingNamedIcon = iconIdIndex[iconInfo.IconId];
                    existingNamedIcon.AddNames(iconInfo.Names);
                } else {
                    iconIdIndex[iconInfo.IconId] = iconInfo;
                    this.iconInfo.Add(iconInfo);
                }
            }
        }
    }

    private void IndexIcons<Row>(
        Func<Row, IEnumerable<int>> icons,
        Func<Row, IEnumerable<SeString?>> names
    ) where Row : ExcelRow {
        IndexIcons(
            (row) => icons(row).Cast<uint>(),
            names
        );
    }

    private void IndexIcons<Row>(
        Func<Row, uint> icon,
        Func<Row, IEnumerable<SeString?>> names
    ) where Row : ExcelRow {
        IndexIcons((row) => new[] { icon(row) }, names);
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
    public List<string> Categories { get; private set; } = new();

    public void AddNames(IEnumerable<string> names) {
        foreach (var name in names) {
            var searchName = name.ToLowerInvariant();
            if (SearchNames.Contains(searchName)) { continue; }

            Names.Add(name);
            SearchNames.Add(searchName);
        }
    }
}
