using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Scripts;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Core;

public static class XyzMaterialCmd {
    public static IReadOnlyList<CardModel> GetMaterials(Creature target) {
        return target.GetPower<XyzMaterialPower>()?.Materials
            ?? Array.Empty<CardModel>();
    }

    public static int GetMaterialCount(Creature target) {
        return GetMaterials(target).Count;
    }

    public static async Task<bool> ReserveForSummon(
        Player owner,
        IReadOnlyList<SummonMaterial> materials
    ) {
        if (CombatManager.Instance.IsOverOrEnding || materials.Count == 0) return false;
        if (materials.Select(material => material.Card).Distinct().Count() != materials.Count
            || materials.Select(material => material.Creature).Distinct().Count() != materials.Count) {
            return false;
        }

        CardPile monsterPile = Entry.MonsterPile.GetPile(owner);
        CardPile xyzPile = Entry.XyzMaterialPile.GetPile(owner);
        List<(BaseMonster Monster, CardModel Card)> reservations = [];

        foreach (SummonMaterial material in materials) {
            if (material is not {
                    Creature: { IsAlive: true } creature,
                    Card: { } card
                }
                || creature.PetOwner != owner
                || !owner.Creature.Pets.Contains(creature)
                || creature.Monster is not BaseMonster monster
                || monster.SourceCard != card
                || card.Owner != owner
                || card.Pile != monsterPile
                || !monster.TryReserveSourceCardAsSummonMaterial(card)) {
                await CancelReservations(owner, reservations);
                return false;
            }

            reservations.Add((monster, card));
        }

        foreach ((_, CardModel card) in reservations) {
            CardPileAddResult result = await CardPileCmd.Add(
                card,
                xyzPile,
                skipVisuals: true
            );
            if (!result.success) {
                await CancelReservations(owner, reservations);
                return false;
            }
        }

        try {
            await Task.WhenAll(reservations.Select(reservation =>
                SummonUtil.MaterialSacrifice(reservation.Monster.Creature)));
            return true;
        }
        catch (Exception ex) {
            Entry.Logger.Error("Failed to consume Xyz summon materials: " + ex);
            await RecoverAfterConsumptionFailure(owner, reservations);
            return false;
        }
    }

    public static async Task<bool> AttachReservedToSummonedMonster(
        SummonPostPlayContext context
    ) {
        Creature? target = context.Owner.Creature.Pets.FirstOrDefault(creature =>
            creature is { IsAlive: true }
            && creature.Monster is BaseMonster { SourceCard: { } sourceCard }
            && sourceCard == context.FinalCard);
        if (target == null || context.FinalCard is not BaseExtraXyzCard) return false;

        List<CardModel> cards = context.Materials
            .Select(material => material.Card)
            .OfType<CardModel>()
            .Distinct()
            .ToList();
        CardPile xyzPile = Entry.XyzMaterialPile.GetPile(context.Owner);
        if (cards.Count != context.Materials.Count
            || cards.Any(card => card.Owner != context.Owner || card.Pile != xyzPile)) {
            return false;
        }

        XyzMaterialPower? power = await PowerCmd.Apply<XyzMaterialPower>(
            context.ChoiceContext,
            target,
            cards.Count,
            context.Owner.Creature,
            context.FinalCard,
            true
        );
        if (power == null || !power.InitializeMaterials(cards)) {
            if (power != null) await PowerCmd.Remove(power);
            return false;
        }

        return true;
    }

    public static async Task<bool> Attach(
        PlayerChoiceContext choiceContext,
        Creature target,
        CardModel card
    ) {
        if (CombatManager.Instance.IsOverOrEnding
            || target is not { IsAlive: true, PetOwner: { } owner }
            || card.Owner != owner
            || target.Monster is not BaseMonster { SourceCard: BaseExtraXyzCard }) {
            return false;
        }

        XyzMaterialPower? power = target.GetPower<XyzMaterialPower>();
        if (power?.Materials.Contains(card) == true) return true;

        CardPileAddResult result = await CardPileCmd.Add(
            card,
            Entry.XyzMaterialPile.GetPile(owner),
            skipVisuals: true
        );
        if (!result.success) return false;

        if (power == null) {
            power = await PowerCmd.Apply<XyzMaterialPower>(
                choiceContext,
                target,
                1,
                owner.Creature,
                card,
                true
            );
            if (power != null && power.InitializeMaterials([card])) return true;
            if (power != null) await PowerCmd.Remove(power);
        }
        else if (power.AttachMaterial(card)) {
            await PowerCmd.ModifyAmount(
                choiceContext,
                power,
                1,
                owner.Creature,
                card,
                true
            );
            return true;
        }

        await CardPileCmd.Add(card, PileType.Discard.GetPile(owner), skipVisuals: true);
        return false;
    }

    public static Task<CardModel?> DetachOne(
        PlayerChoiceContext choiceContext,
        Creature target
    ) {
        CardModel? card = GetMaterials(target).FirstOrDefault();
        return card == null
            ? Task.FromResult<CardModel?>(null)
            : Detach(choiceContext, target, card);
    }

    public static async Task<CardModel?> Detach(
        PlayerChoiceContext choiceContext,
        Creature target,
        CardModel card
    ) {
        XyzMaterialPower? power = target.GetPower<XyzMaterialPower>();
        if (power == null
            || card.Pile?.Type != Entry.XyzMaterialPile
            || !power.DetachMaterial(card)) {
            return null;
        }

        await CardPileCmd.Add(
            card,
            PileType.Discard.GetPile(card.Owner),
            skipVisuals: true
        );
        await PowerCmd.ModifyAmount(
            choiceContext,
            power,
            -1,
            target,
            card,
            true
        );
        return card;
    }

    public static async Task SendAllToGraveyard(
        PlayerChoiceContext choiceContext,
        Creature target
    ) {
        XyzMaterialPower? power = target.GetPower<XyzMaterialPower>();
        if (power != null) {
            await SendAllToGraveyard(choiceContext, power);
            if (target.GetPower<XyzMaterialPower>() == power) {
                await PowerCmd.Remove(power);
            }
        }
    }

    internal static async Task SendAllToGraveyard(
        PlayerChoiceContext choiceContext,
        XyzMaterialPower power
    ) {
        List<CardModel> cards = power.DetachAllMaterials();
        if (CombatManager.Instance.IsOverOrEnding) return;

        foreach (CardModel card in cards) {
            if (card.Pile?.Type == Entry.XyzMaterialPile) {
                await CardPileCmd.Add(
                    card,
                    PileType.Discard.GetPile(card.Owner),
                    skipVisuals: true
                );
            }
        }
    }

    internal static async Task SendReservedToGraveyard(
        Player owner,
        IReadOnlyList<SummonMaterial> materials
    ) {
        if (CombatManager.Instance.IsOverOrEnding) return;
        CardPile xyzPile = Entry.XyzMaterialPile.GetPile(owner);
        foreach (CardModel card in materials
                     .Select(material => material.Card)
                     .OfType<CardModel>()
                     .Distinct()
                     .ToList()) {
            if (card.Pile == xyzPile) {
                await CardPileCmd.Add(
                    card,
                    PileType.Discard.GetPile(owner),
                    skipVisuals: true
                );
            }
        }
    }

    private static async Task CancelReservations(
        Player owner,
        IReadOnlyList<(BaseMonster Monster, CardModel Card)> reservations
    ) {
        CardPile monsterPile = Entry.MonsterPile.GetPile(owner);
        foreach ((BaseMonster monster, CardModel card) in reservations) {
            monster.CancelSourceCardMaterialReservation(card);
            if (card.Pile?.Type == Entry.XyzMaterialPile) {
                await CardPileCmd.Add(card, monsterPile, skipVisuals: true);
            }
        }
    }

    private static async Task RecoverAfterConsumptionFailure(
        Player owner,
        IReadOnlyList<(BaseMonster Monster, CardModel Card)> reservations
    ) {
        if (CombatManager.Instance.IsOverOrEnding) return;

        CardPile monsterPile = Entry.MonsterPile.GetPile(owner);
        CardPile discardPile = PileType.Discard.GetPile(owner);
        foreach ((BaseMonster monster, CardModel card) in reservations) {
            if (card.Pile?.Type != Entry.XyzMaterialPile) continue;

            if (monster.Creature is { IsAlive: true }) {
                monster.CancelSourceCardMaterialReservation(card);
                await CardPileCmd.Add(card, monsterPile, skipVisuals: true);
            }
            else {
                await CardPileCmd.Add(card, discardPile, skipVisuals: true);
            }
        }
    }
}
