using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;

namespace VYgo.Scripts.Actions;

public sealed class RAMClouderAction : BasePerTurnMonsterAction {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override bool IsVisibleInternal => true;
    public override TargetType TargetType => TargetType.None;
    protected override string? IntentIconPath => "res://VYgo/images/powers/reborn.png";
    public override string? CustomIconPath => IntentIconPath;

    public override bool CanAct(ICombatState combatState) {
        return base.CanAct(combatState)
            && Owner.PetOwner is { } player
            && PileType.Discard.GetPile(player).Cards.Any(IsCyberseMonster);
    }

    protected override async Task OnAct(
        PlayerChoiceContext choiceContext,
        Creature? target
    ) {
        if (Owner.PetOwner is not { } player
            || Owner.Monster is not BaseMonster { SourceCard: { } sourceCard }) {
            return;
        }

        BaseMonsterCard? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Discard.GetPile(player),
                player,
                new CardSelectorPrefs(SelectionScreenPrompt, 1),
                IsCyberseMonster))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selected == null) return;

        SpendUses();
        await CreatureCmd.Kill(Owner, true);
        await CardCmd.AutoPlay(choiceContext, selected, null);
    }

    private static bool IsCyberseMonster(CardModel card) {
        return card is BaseMonsterCard monster
            && !monster.IsExtra
            && monster.YgoGetCore().IsRace(YgoRace.Cyberse);
    }
}
