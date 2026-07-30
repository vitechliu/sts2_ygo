using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Actions;

public sealed class CyberDragonInfinityAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        HoverTipFactory.FromPower<NegatingPower>()
    ];
    
    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.None;

    protected override string? IntentIconPath => "res://VYgo/images/intents/negating.png";
    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        var xyzMaterialCount = Owner.GetPowerAmount<XyzMaterialPower>();
        return base.CanAct(combatState) && Owner.PetOwner != null && xyzMaterialCount > 0;
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        Player? player = Owner.PetOwner;
        if (player == null) return;

        var material = await XyzMaterialCmd.DetachOne(choiceContext, Owner);
        if (material == null) {
            //todo 素材警告
            return;
        }
        await PowerCmd.Apply<NegatingPower>(
            choiceContext,
            Owner,
            1m,
            Owner,
            null);
        SpendUses();
    }

}
