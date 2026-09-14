using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Nodes.Screens.Overlays;
using MegaCrit.Sts2.Core.Runs;
using VYgo.Core.Settings;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Scripts.Characters;
using VYgo.Utils;

namespace VYgo.Core.DevConsole;

public sealed partial class VTestConsoleCmd : AbstractConsoleCmd {
    private const string InfinityCommand = "vtest xyz-infinity";
    private static ConsoleCmdGameAction? _pendingAction;
    private static bool _pendingActionStarted;

    public override string CmdName => "vtest";
    public override string Args => "[help | xyz-infinity | xyz-nova | xyz-nova-slow | ritual-single | ritual-dual | ritual-dual-slow | ritual-minimal | ritual-none | ritual-prepare]";
    public override string Description =>
        "VYgo 单机战斗测试。vtest xyz-infinity：自动准备电子龙新星，以其为素材完整超量召唤电子龙无限。" +
        "vtest xyz-nova：准备两只电子龙，按正式规则双素材超量召唤新星。" +
        "xyz-nova-slow 仅将素材展示段慢放五倍，供检查透视与飞出，其他流程相同。" +
        "仪式测试：ritual-single 单素材、ritual-dual 双素材、ritual-dual-slow 退出段慢放、ritual-minimal 快速、ritual-none 无动画、ritual-prepare 准备手动出牌。" +
        "需在 YGO 角色的玩家行动阶段执行，无限需一个随从空位，新星需两个；执行时自动关闭控制台。";
    public override bool IsNetworked => false;

    public override CompletionResult GetArgumentCompletions(Player? player, string[] args) =>
        args.Length <= 1
            ? CompleteArgument(["help", "xyz-infinity", "xyz-nova", "xyz-nova-slow", "ritual-single", "ritual-dual", "ritual-dual-slow", "ritual-minimal", "ritual-none", "ritual-prepare"], [], args.FirstOrDefault() ?? "")
            : base.GetArgumentCompletions(player, args);

    public override CmdResult Process(Player? issuingPlayer, string[] args) {
        if (args.Length == 0 || (args.Length == 1 && args[0].Equals("help", StringComparison.OrdinalIgnoreCase))) {
            return new CmdResult(true, Description);
        }
        if (args.Length == 1 && args[0].StartsWith("ritual-", StringComparison.OrdinalIgnoreCase)) return ProcessRitual(issuingPlayer, args[0].ToLowerInvariant());
        bool slow = args.Length == 1 && args[0].Equals("xyz-nova-slow", StringComparison.OrdinalIgnoreCase);
        bool dual = slow || args.Length == 1 && args[0].Equals("xyz-nova", StringComparison.OrdinalIgnoreCase);
        if (args.Length != 1 || !(dual || args[0].Equals("xyz-infinity", StringComparison.OrdinalIgnoreCase))) {
            return new CmdResult(false, "未知测试参数。用法：" + InfinityCommand + "；帮助：vtest help。");
        }

        string? error = ValidateBattle(issuingPlayer);
        if (error != null) return new CmdResult(false, error);
        if (dual && issuingPlayer!.GetMaxMinionCount() - issuingPlayer.MinionCount() < 2)
            return new CmdResult(false, "双素材测试需要两个随从空位。");

        var run = RunManager.Instance;
        // 原版单机控制台默认直接执行；显式排入原版控制台动作后再执行异步结算。
        if (_pendingAction != null && !_pendingActionStarted
            && ReferenceEquals(run.ActionExecutor.CurrentlyRunningAction, _pendingAction)) {
            _pendingActionStarted = true;
            return new CmdResult(ExecuteInfinity(issuingPlayer!, _pendingAction, dual, slow), true, "正在准备素材并执行完整超量召唤。");
        }
        if (_pendingAction != null || !run.ActionQueueSet.IsEmpty || run.ActionExecutor.IsRunning) {
            return new CmdResult(false, "当前仍有动作或选卡正在结算，请等待完成后再执行测试。");
        }

        var action = new ConsoleCmdGameAction(issuingPlayer!, slow ? "vtest xyz-nova-slow" : dual ? "vtest xyz-nova" : InfinityCommand, inCombat: true);
        _pendingActionStarted = false;
        _pendingAction = action;
        try {
            run.ActionQueueSynchronizer.RequestEnqueue(action);
            return new CmdResult(ObserveCompletion(action), true, "已提交超量测试；结算结果会写回控制台。");
        }
        catch {
            _pendingAction = null;
            throw;
        }
    }

    private static string? ValidateBattle(Player? player) {
        var run = RunManager.Instance;
        var combat = CombatManager.Instance;
        if (!run.IsInProgress || !combat.IsInProgress || combat.IsOverOrEnding
            || player?.Creature.CombatState == null || NCombatRoom.Instance == null) {
            return "测试失败：请先进入有效战斗。";
        }
        if (run.NetService.Type != NetGameType.Singleplayer || !LocalContext.IsMe(player)) {
            return "测试失败：此指令仅支持本机单人对局，不支持联机或回放。";
        }
        if (!player.IsYgoCharacter()) return "测试失败：请使用 VYgo 角色。";
        if (!player.Creature.IsAlive) return "测试失败：玩家当前已死亡。";
        if (combat.PlayerActionsDisabled || !combat.IsPartOfPlayerTurn(player)
            || run.ActionQueueSynchronizer.CombatState != ActionSynchronizerCombatState.PlayPhase
            || run.ActionQueueSet.ActionQueueIsPaused(player.NetId)) {
            return "测试失败：当前不是玩家可行动阶段，请等待回合开始及其他结算完成。";
        }
        if (NOverlayStack.Instance?.ScreenCount > 0) return "测试失败：请先关闭选卡、牌堆或其他弹出界面。";
        if (player.MinionCount() >= player.GetMaxMinionCount()) {
            return $"测试失败：随从区已满（{player.MinionCount()}/{player.GetMaxMinionCount()}），请先留出一个空位。";
        }
        return null;
    }

    private static async Task ObserveCompletion(ConsoleCmdGameAction action) {
        try {
            await action.CompletionTask;
            if (action.Exception != null) Report(false, "测试动作异常，详情见游戏日志。");
        }
        catch (TaskCanceledException) {
            Report(false, "测试动作已取消，请回到有效战斗的玩家行动阶段重试。");
        }
        finally {
            if (ReferenceEquals(_pendingAction, action)) _pendingAction = null;
        }
    }

    private static async Task ExecuteInfinity(Player player, ConsoleCmdGameAction action, bool dual, bool slow) {
        try {
            // 让控制台先完成输入行清理，再隐藏，避免演出被遮挡。
            await Task.Yield();
            string? error = ValidateBattle(player);
            if (error != null) {
                Report(false, error);
                return;
            }
            var novaModel = ModelDb.Card<CyberDragonNova>();
            var infinityModel = ModelDb.Card<CyberDragonInfinity>();
            if (novaModel.YgoGetCore() == null || infinityModel.YgoGetCore()?.Rank is not > 0
                || novaModel.YgoGetMonster() == null || infinityModel.YgoGetMonster() == null) {
                Report(false, "测试失败：新星或无限的卡牌数据/随从模型未正确加载。");
                return;
            }
            foreach (string path in new[] { "res://VYgo/scenes/monsters/58069384.tscn",
                         "res://VYgo/scenes/monsters/10443957.tscn", ExtraDeckSummonAnimations.XyzSummon2DAssets }) {
                if (!ResourceLoader.Exists(path)) {
                    Report(false, "测试失败：缺少召唤场景，请重新发布并重启游戏。");
                    return;
                }
            }

            NDevConsole.Instance?.HideConsole();
            await VYgoModSettings.RunWithFullAnimationForTest(player, async () => {
                var context = new GameActionPlayerChoiceContext(action);
                var combat = player.Creature.CombatState!;
                BaseExtraXyzCard target = dual ? combat.CreateCard<CyberDragonNova>(player) : combat.CreateCard<CyberDragonInfinity>(player);
                BaseMonsterCard[] materials = dual
                    ? [combat.CreateCard<CyberDragon>(player), combat.CreateCard<CyberDragon>(player)]
                    : [combat.CreateCard<CyberDragonNova>(player)];
                var extraPile = Entry.ExtraPile.GetPile(player);
                if (!(await CardPileCmd.Add(target, extraPile, skipVisuals: true)).success) {
                    Report(false, "测试失败：测试卡加入额外卡组失败，已生成的卡保留在当前战斗。");
                    return;
                }
                foreach (var material in materials) {
                    if (!(await CardPileCmd.Add(material, material is BaseExtraXyzCard ? extraPile : PileType.Hand.GetPile(player), skipVisuals: true)).success) {
                        Report(false, "测试失败：素材加入牌堆失败。");
                        return;
                    }
                    var summoned = await material.AutoPlayAndCaptureSummonedCreature(
                        context, null, skipCardPileVisuals: true,
                        playSummonCardFly: false, playMonsterSummonVfx: false);
                    if (summoned is not { IsAlive: true }) {
                        Report(false, "测试失败：素材未能登场，可能被当前战斗效果阻止；测试卡保留在当前战斗。");
                        return;
                    }
                }

                // 只允许本次生成的素材；沿用目标的正式规则与统一预留/挂载流程。
                var spec = SummonUtil.CreateDirectXyzSummonSpec(target, player, (_, _) =>
                    SummonUtil.GetFieldMonsterMaterials(player, material => materials.Contains(material.Card)));
                bool animationCompleted = false;
                var result = await SummonUtil.ExecuteSelectedExtraDeckSummon(new SelectedExtraDeckSummonRequest(
                    SelectedExtraCard: target, Owner: player, ChoiceContext: context,
                    BuildMaterialSelection: spec.BuildMaterialSelection, SummonType: spec.SummonType,
                    PlayAnimation: async animation => {
                        await ExtraDeckSummonAnimations.PlayXyzSummonAnimation(animation, reportFailure: true, slowMaterials: slow);
                        animationCompleted = true;
                    },
                    ConsumeMaterials: spec.ConsumeMaterials, AfterAutoPlay: spec.AfterAutoPlay,
                    OnSummonFailedAfterConsumption: spec.OnSummonFailedAfterConsumption,
                    FinalWaitSeconds: spec.FinalWaitSeconds), materials);

                bool attached = result.SummonedCreature is { IsAlive: true } creature
                    && materials.All(material => XyzMaterialCmd.GetMaterials(creature).Contains(material));
                if (!result.Success || !attached) {
                    Report(false, "测试失败：超量召唤或素材挂载未完成；已发生的结算保留，详情见游戏日志。");
                }
                else if (!animationCompleted) {
                    Report(false, "超量怪兽已登场并挂载素材，但完整演出未完成，详情见游戏日志。");
                }
                else {
                    Report(true, dual ? "电子龙新星已登场，两张电子龙已挂载为超量素材；完整演出调用已完成，请目视核对效果。"
                        : "电子龙无限已登场，新星已挂载为超量素材；完整演出调用已完成，请目视核对效果。");
                }
            });
        }
        catch (Exception ex) {
            Entry.Logger.Error("电子龙无限测试异常：" + ex);
            Report(false, "测试异常中断；临时动画模式已恢复，已发生的结算保留，详情见游戏日志。");
        }
    }

    private static void Report(bool success, string message) {
        string text = "[vtest] " + message;
        if (success) Entry.Logger.Info(text);
        else Entry.Logger.Warn(text);
        NDevConsole.Instance?.GetNode<RichTextLabel>("OutputContainer/OutputBuffer")
            .AppendText(success ? text + "\n" : "[color=#ff5555]" + text + "[/color]\n");
    }
}
