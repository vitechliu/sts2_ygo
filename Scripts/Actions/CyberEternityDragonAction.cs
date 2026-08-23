using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Utils;

namespace VYgo.Scripts.Actions;

public sealed class CyberEternityDragonAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.Enhance(),
    ];

    protected override bool IsVisibleInternal => true;

    public override TargetType TargetType => TargetType.None;

    protected override string? IntentIconPath => "res://VYgo/images/powers/ygo.png";
    public override string? CustomIconPath => IntentIconPath;

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        Player? player = Owner.PetOwner;
        if (player == null) return;

        int fusionCount = PileType.Discard.GetPile(player).Cards
            .OfType<BaseExtraFusionCard>()
            .Count();

        SpendUses();
        if (fusionCount <= 0) return;

        await MinionUtil.AddHp(Owner, (int)Amount * fusionCount);
        await PlayerCmd.GainEnergy(fusionCount, player);
    }
}
