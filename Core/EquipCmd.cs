using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core.Effects;
using VYgo.Scripts;
using VYgo.Scripts.Powers;

namespace VYgo.Core;

public static class EquipCmd {
    public static bool CanEquip(
        CardModel card,
        Creature? target) {
        return TryGetTargetHost(card, target, out _);
    }

    public static bool IsEquipped(
        Player owner,
        CardModel card) {
        return FindHost(owner, card) != null;
    }

    public static bool IsOnField(
        Player owner,
        CardModel card) {
        return card.Pile?.Type == Entry.EquipPile
            && IsEquipped(owner, card);
    }

    public static bool HasEquipment(
        Player owner,
        Func<CardModel, bool>? filter = null) {
        foreach (Creature pet in owner.Creature.Pets) {
            if (pet.GetPower<YgoPower>() is not { } host) continue;
            if (host.EquippedCards.Any(card =>
                    card.Pile?.Type == Entry.EquipPile
                    && (filter == null || filter(card)))) {
                return true;
            }
        }

        return false;
    }

    public static YgoPower? FindHost(
        Player owner,
        CardModel card) {
        foreach (Creature pet in owner.Creature.Pets) {
            YgoPower? host = pet.GetPower<YgoPower>();
            if (host?.EquippedCards.Contains(card) == true) {
                return host;
            }
        }

        return null;
    }

    public static IReadOnlyList<CardModel> GetAllEquipment(Player owner) {
        List<CardModel> cards = [];
        foreach (Creature pet in owner.Creature.Pets) {
            if (pet.GetPower<YgoPower>() is not { } host) continue;
            cards.AddRange(host.EquippedCards.Where(card =>
                card.Pile?.Type == Entry.EquipPile));
        }

        return cards;
    }

    public static async Task<CardModel?> SelectEquipment(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? filter = null) {
        return (await CardSelectCmd.FromCombatPile(
                choiceContext,
                Entry.EquipPile.GetPile(owner),
                owner,
                prefs,
                card => IsOnField(owner, card)
                    && (filter == null || filter(card))))
            .FirstOrDefault();
    }

    public static async Task<CardModel?> SelectAndSendToGraveyard(
        PlayerChoiceContext choiceContext,
        Player owner,
        CardSelectorPrefs prefs,
        Func<CardModel, bool>? filter = null) {
        CardModel? card = await SelectEquipment(
            choiceContext,
            owner,
            prefs,
            filter);
        if (card == null
            || !await SendToGraveyard(choiceContext, card)) {
            return null;
        }

        return card;
    }

    public static Task<bool> AttachPlayedCard(
        PlayerChoiceContext choiceContext,
        CardModel card,
        Creature? target) {
        return Attach(choiceContext, card, target, moveToEquipPile: false);
    }

    public static Task<bool> EquipFromPile(
        PlayerChoiceContext choiceContext,
        CardModel card,
        Creature? target) {
        return Attach(choiceContext, card, target, moveToEquipPile: true);
    }

    public static async Task<bool> SendToGraveyard(
        PlayerChoiceContext choiceContext,
        CardModel card) {
        if (CombatManager.Instance.IsOverOrEnding) return false;

        YgoPower? host = FindHost(card.Owner, card);
        if (host == null || !host.DetachEquipment(card)) return false;

        await ReleaseEquipment(choiceContext, host.Owner, card);
        return true;
    }

    internal static async Task SendAllToGraveyard(
        PlayerChoiceContext choiceContext,
        YgoPower host) {
        List<CardModel> cards = host.DetachAllEquipment();
        if (CombatManager.Instance.IsOverOrEnding) return;

        foreach (CardModel card in cards) {
            await ReleaseEquipment(choiceContext, host.Owner, card);
        }
    }

    private static async Task<bool> Attach(
        PlayerChoiceContext choiceContext,
        CardModel card,
        Creature? target,
        bool moveToEquipPile) {
        if (CombatManager.Instance.IsOverOrEnding
            || !TryGetTargetHost(card, target, out YgoPower? host)) {
            return false;
        }

        YgoPower? currentHost = FindHost(card.Owner, card);
        if (currentHost != null) return currentHost == host;
        if (!host.AttachEquipment(card)) return false;

        if (moveToEquipPile) {
            CardPileAddResult result = await CardPileCmd.Add(
                card,
                Entry.EquipPile.GetPile(card.Owner),
                skipVisuals: true);
            if (!result.success) {
                host.DetachEquipment(card);
                return false;
            }
        }

        await MonsterCardVfx.PlayEquipCardFly(card, target!);

        if (card is IEquipmentEffect effect) {
            await effect.OnEquipped(choiceContext, target!);
        }

        return true;
    }

    private static bool TryGetTargetHost(
        CardModel card,
        Creature? target,
        out YgoPower? host) {
        host = null;
        if (target is not { IsAlive: true }
            || target.PetOwner != card.Owner) {
            return false;
        }

        host = target.GetPower<YgoPower>();
        return host != null;
    }

    private static async Task ReleaseEquipment(
        PlayerChoiceContext choiceContext,
        Creature target,
        CardModel card) {
        if (card is IEquipmentEffect effect) {
            await effect.OnUnequipped(choiceContext, target);
        }

        if (card.Pile?.Type != Entry.EquipPile
            && card.Pile?.Type != PileType.Play) {
            return;
        }

        await CardPileCmd.Add(
            card,
            PileType.Discard.GetPile(card.Owner));
    }
}
