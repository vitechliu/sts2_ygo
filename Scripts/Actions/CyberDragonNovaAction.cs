using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Scripts.Actions;

public sealed class CyberDragonNovaAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.SpecialSummon(),
    ];
    
    protected override bool IsVisibleInternal => true;
    
    public override TargetType TargetType => TargetType.None;

    protected override string? IntentIconPath => "res://VYgo/images/powers/reborn.png";
    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        var xyzMaterialCount = Owner.GetPowerAmount<XyzMaterialPower>();
        return base.CanAct(combatState)
            && Owner.PetOwner is { } player
            && player.MinionCount() < player.GetMaxMinionCount()
            && xyzMaterialCount > 0;
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        Player? player = Owner.PetOwner;
        if (player == null || player.MinionCount() >= player.GetMaxMinionCount()) return;

        var material = await XyzMaterialCmd.DetachOne(choiceContext, Owner);
        if (material == null) {
            //todo 素材警告
            return;
        }
        if ((await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Discard.GetPile(player),
                player: player,
                filter: model => model is BaseMonsterCard bm && bm.YgoGetCore().IsRace(YgoRace.Machine) ))
            .FirstOrDefault() is not { } selectedExtraCard) {
            //todo 警告
            return;
        }
        SpendUses();
        await CardCmd.AutoPlay(choiceContext, selectedExtraCard, null);
    }
}
