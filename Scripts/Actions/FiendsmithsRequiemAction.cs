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
using VYgo.Scripts.Cards.Category.Common;
using VYgo.Utils;

namespace VYgo.Scripts.Actions;

public sealed class FiendsmithsRequiemAction : BasePerTurnMonsterAction {
    protected override bool IsVisibleInternal => true;
    public override TargetType TargetType => TargetType.None;
    protected override string? IntentIconPath => "res://VYgo/images/intents/intent_dragon_vortex.png";
    public override string? CustomIconPath => IntentIconPath;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.SpecialSummon()
    ];

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner is { } player
            && player.MinionCount() < player.GetMaxMinionCount()
            && PileType.Draw.GetPile(player).Cards.Any(FiendsmithUtil.IsLightFiendMonster);
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        Player? player = Owner.PetOwner;
        if (player == null || player.MinionCount() >= player.GetMaxMinionCount()) return;

        BaseMonsterCard? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Draw.GetPile(player),
                player,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                FiendsmithUtil.IsLightFiendMonster))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selected == null || player.MinionCount() >= player.GetMaxMinionCount()) return;

        Creature? summoned = await selected.AutoPlayAndCaptureSummonedCreature(
            choiceContext,
            null);
        if (summoned != null) {
            SpendUses();
        }
    }
}
