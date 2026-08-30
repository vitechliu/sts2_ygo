using System.Security.Cryptography;
using System.Text;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models.Badges;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.Saves.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using STS2RitsuLib;
using VYgo.Core.Saves;
using VYgo.Scripts;

namespace VYgo.Core.Progression;

/// <summary>
/// 将 RitsuLib 的正式对局结束事件转换为全局决斗者档案结算。
/// </summary>
public static class DuelistRunSettlementService {
    private const long BronzeBadgeExperience = 50L;
    private const long SilverBadgeExperience = 100L;
    private const long GoldBadgeExperience = 200L;
    private const long VictoryExperienceBonus = 300L;

    public static void OnRunEnded(RunEndedEvent endedEvent) {
        try {
            if (!TryResolveQualifiedLocalPlayer(endedEvent, out SerializablePlayer? localPlayer)) {
                return;
            }

            if (!YgoSave.Instance.IsReady) {
                Entry.Logger.Warn("跳过决斗者档案结算：Profile 存档尚未就绪。");
                return;
            }

            DuelistRunOutcome outcome = endedEvent.IsAbandoned
                ? DuelistRunOutcome.Abandon
                : endedEvent.IsVictory
                    ? DuelistRunOutcome.Victory
                    : DuelistRunOutcome.Defeat;
            DuelistExperienceBreakdown experience = CalculateExperience(
                endedEvent.Run,
                localPlayer.NetId,
                outcome);
            string runKey = CreateStableRunKey(endedEvent.Run);
            var settlement = new DuelistRunSettlement(runKey, outcome, experience);
            DuelistSettlementResult result = YgoSave.Instance.SettleDuelistRun(settlement);

            if (!result.Applied) {
                Entry.Logger.Info($"跳过重复决斗者档案结算：run={ShortKey(runKey)}。");
                return;
            }

            Entry.Logger.Info(
                $"决斗者档案结算完成：结果={outcome}，run={ShortKey(runKey)}，" +
                $"经验={experience.BaseScore}+{experience.BadgeExperience}+{experience.VictoryBonus}" +
                $"={experience.TotalExperience}，" +
                $"等级=Lv.{result.Before.Level}->Lv.{result.After.Level}，" +
                $"段位={DuelistProgression.FormatRank(result.Before.MajorRankIndex, result.Before.MinorRank)}" +
                $"->{DuelistProgression.FormatRank(result.After.MajorRankIndex, result.After.MinorRank)}。");
        }
        catch (Exception exception) {
            // 档案功能不能阻断原版结算与返回主菜单流程。
            Entry.Logger.Error($"决斗者档案结算失败：{exception}");
        }
    }

    private static bool TryResolveQualifiedLocalPlayer(
        RunEndedEvent endedEvent,
        out SerializablePlayer localPlayer) {
        localPlayer = null!;
        if (TestMode.IsOn) {
            Entry.Logger.Info("跳过决斗者档案结算：测试模式。");
            return false;
        }

        RunManager? runManager = RunManager.Instance;
        if (runManager == null || runManager.State == null) {
            Entry.Logger.Warn("跳过决斗者档案结算：运行时对局状态不可用。");
            return false;
        }

        if (!runManager.ShouldSave) {
            Entry.Logger.Info("跳过决斗者档案结算：不是正式保存的对局。");
            return false;
        }

        if (runManager.NetService.Type == NetGameType.Replay) {
            Entry.Logger.Info("跳过决斗者档案结算：回放模式。");
            return false;
        }

        if (endedEvent.Run.GameMode == GameMode.None) {
            Entry.Logger.Info("跳过决斗者档案结算：对局模式无效。");
            return false;
        }

        if (LocalContext.NetId is not ulong localPlayerId) {
            Entry.Logger.Warn("跳过决斗者档案结算：无法确定本地玩家 ID。");
            return false;
        }

        List<SerializablePlayer> savedMatches = endedEvent.Run.Players
            .Where(player => player.NetId == localPlayerId)
            .ToList();
        var runtimeMatches = runManager.State.Players
            .Where(player => player.NetId == localPlayerId)
            .ToList();
        if (savedMatches.Count != 1 || runtimeMatches.Count != 1) {
            Entry.Logger.Warn(
                $"跳过决斗者档案结算：本地玩家无法唯一确定，" +
                $"存档匹配={savedMatches.Count}，运行时匹配={runtimeMatches.Count}。");
            return false;
        }

        if (!runtimeMatches[0].IsYgoCharacter()) {
            Entry.Logger.Info("跳过决斗者档案结算：本地玩家未使用 VYgo 角色。");
            return false;
        }

        localPlayer = savedMatches[0];
        return true;
    }

    private static DuelistExperienceBreakdown CalculateExperience(
        SerializableRun run,
        ulong localPlayerId,
        DuelistRunOutcome outcome) {
        if (outcome == DuelistRunOutcome.Abandon) {
            return new DuelistExperienceBreakdown(0L, 0, 0, 0, 0L, 0L, 0L);
        }

        bool isVictory = outcome == DuelistRunOutcome.Victory;
        long baseScore = Math.Max(0, ScoreUtility.CalculateScore(run, isVictory));
        List<Badge> badges = ScoreUtility.GetBadges(run, localPlayerId, isVictory);
        int bronzeCount = badges.Count(badge => badge.Rarity == BadgeRarity.Bronze);
        int silverCount = badges.Count(badge => badge.Rarity == BadgeRarity.Silver);
        int goldCount = badges.Count(badge => badge.Rarity == BadgeRarity.Gold);
        long badgeExperience = 0L;
        badgeExperience = DuelistProgression.SaturatingAdd(
            badgeExperience,
            (long)bronzeCount * BronzeBadgeExperience);
        badgeExperience = DuelistProgression.SaturatingAdd(
            badgeExperience,
            (long)silverCount * SilverBadgeExperience);
        badgeExperience = DuelistProgression.SaturatingAdd(
            badgeExperience,
            (long)goldCount * GoldBadgeExperience);
        long victoryBonus = isVictory ? VictoryExperienceBonus : 0L;
        long total = DuelistProgression.SaturatingAdd(baseScore, badgeExperience);
        total = DuelistProgression.SaturatingAdd(total, victoryBonus);
        return new DuelistExperienceBreakdown(
            baseScore,
            bronzeCount,
            silverCount,
            goldCount,
            badgeExperience,
            victoryBonus,
            total);
    }

    /// <summary>
    /// 只使用本局创建后保持稳定的字段，不使用每次保存都会变化的 SaveTime。
    /// </summary>
    private static string CreateStableRunKey(SerializableRun run) {
        string players = string.Join(
            ";",
            run.Players
                .OrderBy(player => player.NetId)
                .Select(player => $"{player.NetId}:{player.CharacterId?.ToString() ?? "none"}"));
        string source = string.Join(
            "|",
            run.StartTime,
            run.SerializableRng?.Seed ?? string.Empty,
            (int)run.GameMode,
            run.PlatformType,
            players);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        return Convert.ToHexString(hash);
    }

    private static string ShortKey(string runKey) {
        return runKey.Length <= 12 ? runKey : runKey[..12];
    }
}
