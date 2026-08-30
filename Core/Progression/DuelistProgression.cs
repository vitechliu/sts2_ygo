using System.Numerics;
using VYgo.Core.Saves;

namespace VYgo.Core.Progression;

public enum DuelistRunOutcome {
    Victory,
    Defeat,
    Abandon,
}

public readonly record struct DuelistExperienceBreakdown(
    long BaseScore,
    int BronzeBadges,
    int SilverBadges,
    int GoldBadges,
    long BadgeExperience,
    long VictoryBonus,
    long TotalExperience);

public readonly record struct DuelistRunSettlement(
    string RunKey,
    DuelistRunOutcome Outcome,
    DuelistExperienceBreakdown Experience);

public readonly record struct DuelistSettlementResult(
    bool Applied,
    DuelistProgressSnapshot Before,
    DuelistProgressSnapshot After,
    DuelistRunSettlement Settlement);

/// <summary>
/// 决斗者等级与段位的纯逻辑规则，不依赖 Godot 或运行时对局状态。
/// </summary>
public static class DuelistProgression {
    public const int MinimumMajorRankIndex = 1;
    public const int MaximumMajorRankIndex = 7;
    public const int MinimumMinorRank = 1;

    private static readonly string[] RankNames = [
        "见习",
        "青铜",
        "白银",
        "黄金",
        "铂金",
        "钻石",
        "决斗王",
    ];

    private static readonly string[] MinorRankRomanNumerals = [
        "I",
        "II",
        "III",
        "IV",
        "V",
    ];

    /// <summary>
    /// 从等级 L 升到 L + 1 所需经验。超过 long 表示范围时显式饱和。
    /// </summary>
    public static long GetExperienceRequired(long level) {
        BigInteger normalizedLevel = BigInteger.Max(BigInteger.One, new BigInteger(level));
        BigInteger required = 500
            + 25 * normalizedLevel
            + 5 * normalizedLevel * normalizedLevel
            + normalizedLevel * normalizedLevel * normalizedLevel / 100;
        return required >= long.MaxValue ? long.MaxValue : (long)required;
    }

    public static int GetMinorRankCount(int majorRankIndex) {
        int normalizedIndex = Math.Clamp(
            majorRankIndex,
            MinimumMajorRankIndex,
            MaximumMajorRankIndex);
        return normalizedIndex <= 2 ? 3 : 5;
    }

    public static string GetRankName(int majorRankIndex) {
        int normalizedIndex = Math.Clamp(
            majorRankIndex,
            MinimumMajorRankIndex,
            MaximumMajorRankIndex);
        return RankNames[normalizedIndex - 1];
    }

    public static string FormatRank(int majorRankIndex, int minorRank) {
        int normalizedIndex = Math.Clamp(
            majorRankIndex,
            MinimumMajorRankIndex,
            MaximumMajorRankIndex);
        int normalizedMinor = Math.Clamp(
            minorRank,
            MinimumMinorRank,
            GetMinorRankCount(normalizedIndex));
        return $"{GetRankName(normalizedIndex)} {normalizedMinor}级";
    }

    public static string FormatMinorRankRoman(int minorRank) {
        int normalizedMinor = Math.Clamp(minorRank, 1, MinorRankRomanNumerals.Length);
        return MinorRankRomanNumerals[normalizedMinor - 1];
    }

    public static DuelistSettlementResult ApplySettlement(
        DuelistProgressData progress,
        DuelistRunSettlement settlement) {
        ArgumentNullException.ThrowIfNull(progress);
        ArgumentException.ThrowIfNullOrWhiteSpace(settlement.RunKey);

        progress.Normalize();
        DuelistProgressSnapshot before = DuelistProgressSnapshot.From(progress);
        if (string.Equals(progress.LastSettledRunKey, settlement.RunKey, StringComparison.Ordinal)) {
            return new DuelistSettlementResult(false, before, before, settlement);
        }

        long grantedExperience = settlement.Outcome == DuelistRunOutcome.Abandon
            ? 0L
            : Math.Max(0L, settlement.Experience.TotalExperience);
        AddExperience(progress, grantedExperience);

        progress.TotalQualifiedRuns = SaturatingIncrement(progress.TotalQualifiedRuns);
        switch (settlement.Outcome) {
            case DuelistRunOutcome.Victory:
                ApplyVictory(progress, before);
                break;
            case DuelistRunOutcome.Defeat:
                progress.TotalLosses = SaturatingIncrement(progress.TotalLosses);
                ApplyLoss(progress);
                break;
            case DuelistRunOutcome.Abandon:
                // 放弃属于失败；TotalAbandons 是其中可单独分析的子集。
                progress.TotalLosses = SaturatingIncrement(progress.TotalLosses);
                progress.TotalAbandons = SaturatingIncrement(progress.TotalAbandons);
                ApplyLoss(progress);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(settlement));
        }

        progress.LastSettledRunKey = settlement.RunKey;
        progress.Normalize();
        DuelistProgressSnapshot after = DuelistProgressSnapshot.From(progress);
        MergeLevelPresentation(progress, before, after);
        return new DuelistSettlementResult(true, before, after, settlement);
    }

    private static void AddExperience(DuelistProgressData progress, long amount) {
        progress.Experience = SaturatingAdd(progress.Experience, amount);

        while (progress.Level < long.MaxValue) {
            long required = GetExperienceRequired(progress.Level);
            if (progress.Experience < required) break;

            progress.Experience -= required;
            progress.Level++;
        }
    }

    private static void ApplyVictory(
        DuelistProgressData progress,
        DuelistProgressSnapshot before) {
        progress.TotalWins = SaturatingIncrement(progress.TotalWins);
        progress.CurrentWinStreak = SaturatingIncrement(progress.CurrentWinStreak);
        progress.BestWinStreak = Math.Max(progress.BestWinStreak, progress.CurrentWinStreak);

        int maxMinor = GetMinorRankCount(progress.MajorRankIndex);
        bool isMaximumRank = progress.MajorRankIndex == MaximumMajorRankIndex
            && progress.MinorRank == maxMinor;
        if (!isMaximumRank) {
            if (progress.MinorRank < maxMinor) {
                progress.MinorRank++;
            }
            else {
                progress.MajorRankIndex++;
                progress.MinorRank = MinimumMinorRank;
            }
        }

        if (progress.MajorRankIndex != before.MajorRankIndex
            || progress.MinorRank != before.MinorRank) {
            DuelistRankUpPresentationData? pending = progress.PendingRankUpPresentation;
            progress.PendingRankUpPresentation = new DuelistRankUpPresentationData {
                FromMajorRankIndex = pending?.FromMajorRankIndex ?? before.MajorRankIndex,
                FromMinorRank = pending?.FromMinorRank ?? before.MinorRank,
                ToMajorRankIndex = progress.MajorRankIndex,
                ToMinorRank = progress.MinorRank,
            };
        }
    }

    private static void ApplyLoss(DuelistProgressData progress) {
        progress.MinorRank = MinimumMinorRank;
        progress.CurrentWinStreak = 0L;
    }

    private static void MergeLevelPresentation(
        DuelistProgressData progress,
        DuelistProgressSnapshot before,
        DuelistProgressSnapshot after) {
        if (after.Level <= before.Level) return;

        DuelistLevelUpPresentationData? pending = progress.PendingLevelUpPresentation;
        progress.PendingLevelUpPresentation = new DuelistLevelUpPresentationData {
            FromLevel = pending?.FromLevel ?? before.Level,
            ToLevel = after.Level,
        };
    }

    internal static long SaturatingAdd(long left, long right) {
        left = Math.Max(0L, left);
        right = Math.Max(0L, right);
        return left > long.MaxValue - right ? long.MaxValue : left + right;
    }

    internal static long SaturatingIncrement(long value) {
        value = Math.Max(0L, value);
        return value == long.MaxValue ? long.MaxValue : value + 1L;
    }
}
