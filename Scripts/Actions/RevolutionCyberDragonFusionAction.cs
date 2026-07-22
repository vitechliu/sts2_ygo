using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Scripts.Cards;

namespace VYgo.Scripts.Actions;

public sealed class RevolutionCyberDragonFusionAction : BasePerTurnMonsterAction {
    private const string MachineRace = "机械族";
    
    
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.FusionSummon(),
        YgoHoverTipConst.Action()
    ];
    
    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.None;

    protected override string? IntentIconPath => "res://VYgo/images/intents/intent_dragon_vortex.png";
    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState) && Owner.PetOwner != null;
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        Player? player = Owner.PetOwner;
        if (player == null) return;

        CardPile extraPile = Entry.ExtraPile.GetPile(player);
        int extraDeckCountBefore = extraPile.Cards.Count;

        SpendUses();
        await SummonUtil.ExecuteFusionSummon(new FusionSummonRequest(
            SourceCard: null,
            Owner: player,
            ChoiceContext: choiceContext,
            SelectionPrompt: SelectionScreenPrompt,
            GetAvailableMaterials: _ => SummonUtil.GetFieldAndHandMonsterMaterials(player),
            FusionCardFilter: IsMachineFusionMonster
        ));

        if (Owner.IsAlive && extraPile.Cards.Count >= extraDeckCountBefore) {
            RecoverUses();
        }
    }

    private static bool IsMachineFusionMonster(BaseExtraFusionCard card) {
        return card.YgoGetCore()?.Race == MachineRace;
    }
}
