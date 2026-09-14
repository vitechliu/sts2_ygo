using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Cards;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using VYgo.Core.Effects;
using VYgo.Core.Settings;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Utils;
namespace VYgo.Core;

public sealed record RitualSummonResult(bool Success, Creature? Creature, IReadOnlyList<SummonMaterial> Materials, bool AnimationCompleted);
public static class RitualSummonCmd
{
    public static SummonMaterialSelectionSpec BuildSelection(BaseRitualMonsterCard target)
    {
        var owner = target.Owner;
        var candidates = SummonUtil.GetFieldMonsterMaterials(owner)
            .Concat(PileType.Hand.GetPile(owner).Cards.Where(c => c != target && SummonMaterial.IsHandMonsterCard(c)).Select(SummonMaterial.FromHandMonsterCard))
            .Where(m => m.Level is > 0).ToList();
        bool validTarget = target.Pile == PileType.Hand.GetPile(owner) && target.Pile.Cards.Contains(target) && target.Level is > 0;
        return new SummonMaterialSelectionSpec(candidates, 1, null, ms => validTarget && ms.Sum(m => m.Level ?? 0) == target.Level
            && SummonUtil.CanSummonWithMaterials(owner, ms));
    }
    public static Task<RitualSummonResult> Execute(BaseRitualMonsterCard target, PlayerChoiceContext context, CardModel? source = null) => Execute(target, context, source, null, false);

    private static readonly HashSet<BaseRitualMonsterCard> Executing = [];

    // 调试入口只指定来源战斗卡，规则、消费和怪兽生命周期与正式魔法完全相同。
    internal static async Task<RitualSummonResult> Execute(BaseRitualMonsterCard target, PlayerChoiceContext context, CardModel? source,
        IReadOnlyList<CardModel>? preselected, bool slowExit)
    {
        if (!Executing.Add(target)) return new(false, null, [], false);
        try { return await ExecuteCore(target, context, source, preselected, slowExit); }
        finally { Executing.Remove(target); }
    }

    private static async Task<RitualSummonResult> ExecuteCore(BaseRitualMonsterCard target, PlayerChoiceContext context, CardModel? source,
        IReadOnlyList<CardModel>? preselected, bool slowExit)
    {
        var owner = target.Owner; var combat = owner.Creature.CombatState;
        RitualSummonResult Failed(IReadOnlyList<SummonMaterial>? ms = null) => new(false, null, ms ?? [], false);
        if (combat == null || CombatManager.Instance.IsOverOrEnding || !owner.Creature.IsAlive) return Failed();
        var selected = preselected != null ? BuildSelection(target).ResolveMaterials(preselected)
            : await SummonMaterialSelectCmd.Select(context, owner, target, () => BuildSelection(target));
        var spec = BuildSelection(target); var materials = spec.ResolveMaterials(selected.Select(m => m.Card!));
        if (!spec.IsValidSelection(materials) || preselected != null && preselected.Count != materials.Count) return Failed(materials);
        NCard? sourceNode = source == null ? null : NCard.FindOnTable(source); bool sourceVisible = sourceNode?.Visible == true;
        if (sourceNode != null) sourceNode.Visible = false;
        bool animationCompleted = false;
        try
        {
            if (!await SummonUtil.ConsumeSummonMaterials(context, owner, materials, _ => PileType.Discard, materialAccent: RitualSummonAnimations.Accent)) return Failed(materials);
            EffectMode mode = VYgoModSettings.GetEffectMode(owner);
            if (mode == EffectMode.full && NCombatRoom.Instance != null)
            {
                try { await RitualSummonAnimations.Play(target, materials.Select(m => m.Card!).ToList(), slowExit); animationCompleted = true; }
                catch (Exception ex) when (ex is not TaskCanceledException) { Entry.Logger.Warn("仪式演出失败，继续玩法结算：" + ex); }
            }
            // 素材死亡 Hook 可能结束战斗、移走目标或占用最后一个位置。
            if (CombatManager.Instance.IsOverOrEnding || owner.Creature.CombatState != combat || !owner.Creature.IsAlive
                || target.Pile != PileType.Hand.GetPile(owner) || owner.MinionCount() >= owner.GetMaxMinionCount()) return Failed(materials);
            Creature? summoned = await target.ResolveRitual(context);
            if (summoned is { IsAlive: true } && mode == EffectMode.minimal)
            {
                try { await MonsterCardVfx.PlaySummonCardFly(target, summoned, revealColor: RitualSummonAnimations.Accent, emphasizeReveal: true); animationCompleted = true; }
                catch (Exception ex) { Entry.Logger.Warn("快速仪式出卡失败：" + ex); }
            }
            bool success = summoned is { IsAlive: true };
            Entry.Logger.Info($"[ritual] 结果={success} 模式={mode} 素材数={materials.Count} 素材星级={materials.Sum(m => m.Level ?? 0)} 目标来源牌堆={target.Pile?.Type} 生命={summoned?.CurrentHp}/{target.Life} 手牌残影={NCombatRoom.Instance?.Ui.Hand.GetCardHolder(target) != null} 演出完成={animationCompleted}");
            return new(success, summoned, materials, animationCompleted);
        }
        finally { if (GodotObject.IsInstanceValid(sourceNode)) sourceNode!.Visible = sourceVisible; }
    }
}
