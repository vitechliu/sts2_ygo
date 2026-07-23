using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using VYgo.Core;

namespace VYgo.Scripts.Cards;

public abstract class BaseEquipSpellCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseSpellCard(baseCost, CardType.Skill, rarity, target, showInCardLibrary),
        IEquipmentEffect {

    protected sealed override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay) {
        if (cardPlay.ResultPile != Entry.EquipPile) return;
        if (!await EquipCmd.AttachPlayedCard(
                choiceContext,
                this,
                cardPlay.Target)) {
            await CardPileCmd.Add(this, PileType.Discard.GetPile(Owner));
        }
    }

    protected virtual Task OnEquipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        return Task.CompletedTask;
    }

    protected virtual Task OnUnequipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        return Task.CompletedTask;
    }

    Task IEquipmentEffect.OnEquipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        return OnEquipped(choiceContext, target);
    }

    Task IEquipmentEffect.OnUnequipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        return OnUnequipped(choiceContext, target);
    }

    protected override CardLocation GetResultLocationForCardPlay() {
        CardLocation fallback = base.GetResultLocationForCardPlay();
        if (fallback.pileType != PileType.Discard) return fallback;

        return EquipCmd.CanEquip(this, CurrentTarget)
            ? new CardLocation(Owner, Entry.EquipPile, CardPilePosition.Bottom)
            : fallback;
    }
}
