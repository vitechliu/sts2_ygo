using Godot;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Runs;
using VYgo.Core.Settings;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Common;
using VYgo.Scripts.Cards.Category.ZaneTruesdale;
using VYgo.Scripts.Monsters;
using VYgo.Utils;
namespace VYgo.Core.DevConsole;

public sealed partial class VTestConsoleCmd
{
    private CmdResult ProcessRitual(Player? player, string command)
    {
        if (command is not ("ritual-single" or "ritual-dual" or "ritual-dual-slow" or "ritual-minimal" or "ritual-none" or "ritual-prepare")) return new CmdResult(false, "未知仪式参数，请查看 vtest help。");
        string? error = ValidateBattle(player); if (error != null) return new CmdResult(false, error);
        int needed = command == "ritual-single" ? 2 : 3;
        if (PileType.Hand.GetPile(player!).Cards.Count + needed > CardPile.MaxCardsInHand)
            return new CmdResult(false, $"测试准备需要 {needed} 个手牌空位，请先腾出空间。");
        var run = RunManager.Instance;
        if (_pendingAction != null && !_pendingActionStarted && ReferenceEquals(run.ActionExecutor.CurrentlyRunningAction, _pendingAction))
        {
            _pendingActionStarted = true; return new CmdResult(ExecuteRitual(player!, _pendingAction, command), true, "正在执行正式仪式测试。");
        }
        if (_pendingAction != null || !run.ActionQueueSet.IsEmpty || run.ActionExecutor.IsRunning) return new CmdResult(false, "请等待当前动作和选卡完成。");
        var action = new ConsoleCmdGameAction(player!, "vtest " + command, inCombat: true); _pendingAction = action; _pendingActionStarted = false;
        try { run.ActionQueueSynchronizer.RequestEnqueue(action); return new CmdResult(ObserveCompletion(action), true, "仪式测试已排队。"); }
        catch { _pendingAction = null; throw; }
    }
    private static async Task ExecuteRitual(Player player, ConsoleCmdGameAction action, string command)
    {
        try
        {
            await Task.Yield(); string? error = ValidateBattle(player); if (error != null) { Report(false, error); return; }
            foreach (string path in new[] { RitualSummonAnimations.ScenePath, "res://VYgo/scenes/monsters/5405694.tscn", "res://VYgo/images/cards/5405694.png", "res://VYgo/images/cards/55761792.png" })
                if (!ResourceLoader.Exists(path)) { Report(false, "缺少仪式资源，请发布并重启游戏：" + path); return; }
            NDevConsole.Instance?.HideConsole();
            var mode = command == "ritual-none" ? EffectMode.none : command == "ritual-minimal" ? EffectMode.minimal : EffectMode.full;
            await VYgoModSettings.RunWithAnimationForTest(player, mode, async () =>
            {
                var context = new GameActionPlayerChoiceContext(action); var combat = player.Creature.CombatState!;
                int before = player.MinionCount();
                var target = combat.CreateCard<BlackLusterSoldier>(player);
                BaseMonsterCard[] cards = command == "ritual-single" ? [combat.CreateCard<BlackLusterSoldier>(player)] : [combat.CreateCard<CyberDragon>(player), combat.CreateCard<ProtoCyberDragon>(player)];
                foreach (var card in cards.Prepend(target))
                    if (!(await CardPileCmd.Add(card, PileType.Hand.GetPile(player), skipVisuals: command != "ritual-prepare")).success) { Report(false, "测试卡加入手牌失败。"); return; }
                if (cards.Length == 2)
                {
                    var field = await cards[0].AutoPlayAndCaptureSummonedCreature(context, null, skipCardPileVisuals: command != "ritual-prepare", playSummonCardFly: false, playMonsterSummonVfx: false);
                    if (field is not { IsAlive: true }) { Report(false, "场上素材准备失败。"); return; }
                }
                var selection = RitualSummonCmd.BuildSelection(target); var selected = selection.ResolveMaterials(cards);
                if (!selection.IsValidSelection(selected) || selected.Sum(m => m.Level ?? 0) != 8) { Report(false, "样例素材不满足正式的合计 8 星规则。"); return; }
                if (command == "ritual-prepare")
                {
                    var spell = combat.CreateCard<BlackLusterRitual>(player); await CardPileCmd.Add(spell, PileType.Hand.GetPile(player), skipVisuals: false);
                    Report(true, "已准备混沌战士、混沌的仪式、场上 5 星电子龙及手牌 3 星原始电子龙。请手动打出仪式魔法并选择素材。"); return;
                }
                var result = await RitualSummonCmd.Execute(target, context, null, cards, command == "ritual-dual-slow");
                bool discarded = cards.All(c => c.Pile?.Type == PileType.Discard);
                bool source = result.Creature?.Monster is BaseMonster m && m.SourceCard == target && target.Pile?.Type == Entry.MonsterPile;
                bool stats = result.Creature?.CurrentHp == target.Life;
                bool count = player.MinionCount() == before + 1;
                Report(result.Success && discarded && source && stats && count,
                    $"仪式模式={mode}；素材={cards.Length}张/8星；素材入弃牌堆={discarded}；结果来源牌堆正确={source}；生命={result.Creature?.CurrentHp}/{target.Life}；随从数={player.MinionCount()}（预期{before + 1}）；演出调用完成={result.AnimationCompleted}。视觉需另行核对。");
            });
        }
        catch (Exception ex) { Entry.Logger.Error("仪式测试异常：" + ex); Report(false, "仪式测试异常，临时模式已恢复；已发生的结算保留，详情见日志。"); }
    }
}
