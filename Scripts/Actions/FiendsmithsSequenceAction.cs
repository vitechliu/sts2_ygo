using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Common;

namespace VYgo.Scripts.Actions;

public sealed class FiendsmithsSequenceAction : BasePerTurnMonsterAction {
    protected override bool IsVisibleInternal => true;
    public override TargetType TargetType => TargetType.None;
    protected override string? IntentIconPath => "res://VYgo/images/intents/intent_dragon_vortex.png";
    public override string? CustomIconPath => IntentIconPath;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.FusionSummon()
    ];

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner is { } player
            && SummonUtil.HasFusionSummonTarget(
                player,
                _ => GetMaterials(player),
                _ => PileType.Draw,
                IsFiendFusionMonster);
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        Player? player = Owner.PetOwner;
        if (player == null) return;

        SpendUses();
        ExtraDeckSummonResult result = await SummonUtil.ExecuteFusionSummon(
            new FusionSummonRequest(
                SourceCard: null,
                Owner: player,
                ChoiceContext: choiceContext,
                SelectionPrompt: SelectionScreenPrompt,
                GetAvailableMaterials: _ => GetMaterials(player),
                GetMaterialDestination: _ => PileType.Draw,
                FusionCardFilter: IsFiendFusionMonster));
        if (!result.Success && Owner.IsAlive) {
            RecoverUses();
        }
    }

    private static IReadOnlyList<SummonMaterial> GetMaterials(Player player) {
        return SummonUtil.GetMonsterMaterialsFromPiles(
            player,
            [PileType.Discard]);
    }

    private static bool IsFiendFusionMonster(BaseExtraFusionCard card) {
        return card.YgoGetCore().IsRace(YgoRace.Fiend);
    }
}
