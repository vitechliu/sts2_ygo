using VYgo.Core.Progression;

namespace VYgo.Core.Saves;

/// <summary>
/// VYgo 的 Profile 级持久化入口。
/// </summary>
public sealed class YgoSave : ProfileSave<YgoSaveData> {
    public const string DataKey = "profile_progress";
    public const string FileName = "profile_progress.json";

    public static YgoSave Instance { get; } = new();

    private YgoSave() : base(Scripts.Entry.ModId, DataKey, FileName) { }

    /// <summary>
    /// 档案尚未就绪时返回 false；否则返回全局决斗者档案的只读快照。
    /// </summary>
    public bool TryGetDuelistProgress(out DuelistProgressSnapshot progress) {
        return TryRead(data => {
            data.Normalize();
            return DuelistProgressSnapshot.From(data.DuelistProgress);
        }, out progress);
    }

    /// <summary>
    /// 幂等结算一局正式对局，并立即保存 Profile。
    /// </summary>
    public DuelistSettlementResult SettleDuelistRun(DuelistRunSettlement settlement) {
        DuelistSettlementResult result = default;
        Modify(data => {
            data.Normalize();
            result = DuelistProgression.ApplySettlement(data.DuelistProgress, settlement);
        });
        return result;
    }

    /// <summary>
    /// 读取并一次性清除待展示的升级/升段结果。Profile 尚未就绪时返回 false。
    /// </summary>
    public bool TryConsumeDuelistPresentation(
        out DuelistProgressSnapshot progress,
        out DuelistPresentationSnapshot presentation) {
        progress = DuelistProgressSnapshot.Default;
        presentation = DuelistPresentationSnapshot.Empty;
        if (!TryRead(data => {
                data.Normalize();
                return data.DuelistProgress.PendingLevelUpPresentation != null
                    || data.DuelistProgress.PendingRankUpPresentation != null;
            }, out bool hasPresentation)) {
            return false;
        }

        if (!hasPresentation) {
            TryGetDuelistProgress(out progress);
            return true;
        }

        DuelistProgressSnapshot consumedProgress = DuelistProgressSnapshot.Default;
        DuelistPresentationSnapshot consumedPresentation = DuelistPresentationSnapshot.Empty;
        Modify(data => {
            data.Normalize();
            DuelistProgressData dataProgress = data.DuelistProgress;
            consumedProgress = DuelistProgressSnapshot.From(dataProgress);
            consumedPresentation = DuelistPresentationSnapshot.From(dataProgress);
            dataProgress.PendingLevelUpPresentation = null;
            dataProgress.PendingRankUpPresentation = null;
        });
        progress = consumedProgress;
        presentation = consumedPresentation;
        return true;
    }

    /// <summary>
    /// 获取指定角色的进度快照。characterId 必须是稳定 ID，不能使用本地化名称。
    /// </summary>
    public CharacterProgressSnapshot GetCharacterProgress(string characterId) {
        ValidateCharacterId(characterId);
        return Read(data => CreateSnapshot(data, characterId));
    }

    /// <summary>
    /// 档案尚未就绪时返回 false；角色没有记录时返回一级零经验的默认快照。
    /// </summary>
    public bool TryGetCharacterProgress(string characterId, out CharacterProgressSnapshot progress) {
        ValidateCharacterId(characterId);
        return TryRead(data => CreateSnapshot(data, characterId), out progress);
    }

    /// <summary>
    /// 修改指定角色的进度。首次修改该角色时会自动创建默认记录。
    /// </summary>
    public void ModifyCharacterProgress(
        string characterId,
        Action<CharacterProgressData> modifier,
        bool saveImmediately = true) {
        ValidateCharacterId(characterId);
        ArgumentNullException.ThrowIfNull(modifier);

        Modify(data => {
            data.Normalize();
            if (!data.Characters.TryGetValue(characterId, out CharacterProgressData? progress)) {
                progress = new CharacterProgressData();
                data.Characters[characterId] = progress;
            }

            modifier(progress);
            progress.Normalize();
        }, saveImmediately);
    }

    private static CharacterProgressSnapshot CreateSnapshot(YgoSaveData data, string characterId) {
        data.Normalize();
        return data.Characters.TryGetValue(characterId, out CharacterProgressData? progress)
            ? new CharacterProgressSnapshot(
                Math.Max(CharacterProgressData.MinimumLevel, progress.Level),
                Math.Max(0L, progress.Experience))
            : CharacterProgressSnapshot.Default;
    }

    private static void ValidateCharacterId(string characterId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
    }
}

/// <summary>
/// VYgo Profile 存档的 JSON 根对象。
/// 新增持久化概念时优先在这里增加带默认值的 public property，并保持旧属性名不变。
/// </summary>
public sealed class YgoSaveData {
    public Dictionary<string, CharacterProgressData> Characters { get; set; } = new();
    public DuelistProgressData DuelistProgress { get; set; } = new();

    // 为简单的解锁、开关和累计值预留强类型容器；复杂数据应新增专用 class/property。
    public HashSet<string> UnlockedContentIds { get; set; } = [];
    public Dictionary<string, bool> Flags { get; set; } = new();
    public Dictionary<string, long> Counters { get; set; } = new();

    internal void Normalize() {
        Characters ??= new();
        DuelistProgress ??= new();
        DuelistProgress.Normalize();
        UnlockedContentIds ??= [];
        Flags ??= new();
        Counters ??= new();
    }
}

/// <summary>
/// 所有 VYgo 角色共享的 Profile 级决斗者档案。
/// </summary>
public sealed class DuelistProgressData {
    public const long MinimumLevel = 1L;

    public long Level { get; set; } = MinimumLevel;
    public long Experience { get; set; }
    public int MajorRankIndex { get; set; } = DuelistProgression.MinimumMajorRankIndex;
    public int MinorRank { get; set; } = DuelistProgression.MinimumMinorRank;
    public long TotalQualifiedRuns { get; set; }
    public long TotalWins { get; set; }
    public long TotalLosses { get; set; }
    public long TotalAbandons { get; set; }
    public long CurrentWinStreak { get; set; }
    public long BestWinStreak { get; set; }
    public string LastSettledRunKey { get; set; } = string.Empty;
    public DuelistLevelUpPresentationData? PendingLevelUpPresentation { get; set; }
    public DuelistRankUpPresentationData? PendingRankUpPresentation { get; set; }

    internal void Normalize() {
        Level = Math.Max(MinimumLevel, Level);
        Experience = Math.Max(0L, Experience);
        MajorRankIndex = Math.Clamp(
            MajorRankIndex,
            DuelistProgression.MinimumMajorRankIndex,
            DuelistProgression.MaximumMajorRankIndex);
        MinorRank = Math.Clamp(
            MinorRank,
            DuelistProgression.MinimumMinorRank,
            DuelistProgression.GetMinorRankCount(MajorRankIndex));
        TotalQualifiedRuns = Math.Max(0L, TotalQualifiedRuns);
        TotalWins = Math.Max(0L, TotalWins);
        TotalLosses = Math.Max(0L, TotalLosses);
        TotalAbandons = Math.Max(0L, TotalAbandons);
        CurrentWinStreak = Math.Max(0L, CurrentWinStreak);
        BestWinStreak = Math.Max(Math.Max(0L, BestWinStreak), CurrentWinStreak);
        LastSettledRunKey ??= string.Empty;
        PendingLevelUpPresentation?.Normalize();
        PendingRankUpPresentation?.Normalize();
    }
}

public sealed class DuelistLevelUpPresentationData {
    public long FromLevel { get; set; } = DuelistProgressData.MinimumLevel;
    public long ToLevel { get; set; } = DuelistProgressData.MinimumLevel;

    internal void Normalize() {
        FromLevel = Math.Max(DuelistProgressData.MinimumLevel, FromLevel);
        ToLevel = Math.Max(FromLevel, ToLevel);
    }
}

public sealed class DuelistRankUpPresentationData {
    public int FromMajorRankIndex { get; set; } = DuelistProgression.MinimumMajorRankIndex;
    public int FromMinorRank { get; set; } = DuelistProgression.MinimumMinorRank;
    public int ToMajorRankIndex { get; set; } = DuelistProgression.MinimumMajorRankIndex;
    public int ToMinorRank { get; set; } = DuelistProgression.MinimumMinorRank;

    internal void Normalize() {
        FromMajorRankIndex = Math.Clamp(
            FromMajorRankIndex,
            DuelistProgression.MinimumMajorRankIndex,
            DuelistProgression.MaximumMajorRankIndex);
        ToMajorRankIndex = Math.Clamp(
            ToMajorRankIndex,
            DuelistProgression.MinimumMajorRankIndex,
            DuelistProgression.MaximumMajorRankIndex);
        FromMinorRank = Math.Clamp(
            FromMinorRank,
            DuelistProgression.MinimumMinorRank,
            DuelistProgression.GetMinorRankCount(FromMajorRankIndex));
        ToMinorRank = Math.Clamp(
            ToMinorRank,
            DuelistProgression.MinimumMinorRank,
            DuelistProgression.GetMinorRankCount(ToMajorRankIndex));
    }
}

public readonly record struct DuelistProgressSnapshot(
    long Level,
    long Experience,
    long ExperienceRequired,
    int MajorRankIndex,
    int MinorRank,
    long TotalQualifiedRuns,
    long TotalWins,
    long TotalLosses,
    long TotalAbandons,
    long CurrentWinStreak,
    long BestWinStreak) {
    public static DuelistProgressSnapshot Default { get; } = From(new DuelistProgressData());

    internal static DuelistProgressSnapshot From(DuelistProgressData progress) {
        progress.Normalize();
        return new DuelistProgressSnapshot(
            progress.Level,
            progress.Experience,
            DuelistProgression.GetExperienceRequired(progress.Level),
            progress.MajorRankIndex,
            progress.MinorRank,
            progress.TotalQualifiedRuns,
            progress.TotalWins,
            progress.TotalLosses,
            progress.TotalAbandons,
            progress.CurrentWinStreak,
            progress.BestWinStreak);
    }
}

public readonly record struct DuelistPresentationSnapshot(
    DuelistLevelUpPresentationData? LevelUp,
    DuelistRankUpPresentationData? RankUp) {
    public static DuelistPresentationSnapshot Empty { get; } = new(null, null);

    internal static DuelistPresentationSnapshot From(DuelistProgressData progress) {
        return new DuelistPresentationSnapshot(
            progress.PendingLevelUpPresentation,
            progress.PendingRankUpPresentation);
    }
}

/// <summary>
/// 单个角色的长期进度。后续角色专属内容可继续增加带默认值的属性。
/// </summary>
public sealed class CharacterProgressData {
    public const int MinimumLevel = 1;

    public int Level { get; set; } = MinimumLevel;
    public long Experience { get; set; }
    public HashSet<string> UnlockedContentIds { get; set; } = [];
    public Dictionary<string, long> Counters { get; set; } = new();

    internal void Normalize() {
        Level = Math.Max(MinimumLevel, Level);
        Experience = Math.Max(0L, Experience);
        UnlockedContentIds ??= [];
        Counters ??= new();
    }
}

/// <summary>
/// 提供给 UI 和玩法代码的只读角色进度快照，避免外部绕过 Save 修改活动存档对象。
/// </summary>
public readonly record struct CharacterProgressSnapshot(int Level, long Experience) {
    public static CharacterProgressSnapshot Default { get; } =
        new(CharacterProgressData.MinimumLevel, 0L);
}
