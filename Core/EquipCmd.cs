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
        return IsValidTarget(card, target)
            && FindPower(card.Owner, card) == null;
    }

    public static bool IsEquipped(
        Player owner,
        CardModel card) {
        return FindPower(owner, card) != null;
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
            foreach (EquipmentPower power in pet.GetPowerInstances<EquipmentPower>()) {
                if (power.EquipmentCard is { } card
                    && card.Pile?.Type == Entry.EquipPile
                    && (filter == null || filter(card))) {
                    return true;
                }
            }
        }

        return false;
    }

    public static EquipmentPower? FindPower(
        Player owner,
        CardModel card) {
        foreach (Creature pet in owner.Creature.Pets) {
            foreach (EquipmentPower power in pet.GetPowerInstances<EquipmentPower>()) {
                if (power.EquipmentCard == card) {
                    return power;
                }
            }
        }

        return null;
    }

    public static IReadOnlyList<CardModel> GetAllEquipment(Player owner) {
        List<CardModel> cards = [];
        foreach (Creature pet in owner.Creature.Pets) {
            cards.AddRange(pet.GetPowerInstances<EquipmentPower>()
                .Select(power => power.EquipmentCard)
                .OfType<CardModel>()
                .Where(card => card.Pile?.Type == Entry.EquipPile));
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

    /// <summary>
    /// 重放装备卡时，在同一张装备与同一个能力上再次应用装备效果。
    /// 装备卡本身仍只计为一张，解除装备时会按累计次数完整回退效果。
    /// </summary>
    public static async Task<bool> ReapplyPlayedCard(
        PlayerChoiceContext choiceContext,
        CardModel card,
        Creature? target) {
        if (CombatManager.Instance.IsOverOrEnding
            || !IsValidTarget(card, target)
            || FindPower(card.Owner, card) is not { } power
            || power.Owner != target) {
            return false;
        }

        if (card is not IEquipmentEffect effect) return true;

        await effect.OnEquipped(choiceContext, target!);
        if (power.RecordEffectApplication(card)) return true;

        // 能力在异步装备效果结算期间被移除时，立即回退刚刚应用的这一次效果。
        await effect.OnUnequipped(choiceContext, target!);
        return false;
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

        EquipmentPower? power = FindPower(card.Owner, card);
        if (power == null) return false;

        await ReleaseEquipmentPower(
            choiceContext,
            power,
            removePower: true);
        return true;
    }

    internal static async Task ReleaseEquipmentPower(
        PlayerChoiceContext choiceContext,
        EquipmentPower power,
        bool removePower,
        Creature? knownOwner = null) {
        CardModel? card = power.TakeEquipmentCard(out int effectApplicationCount);
        if (card == null) return;

        Creature target = knownOwner ?? power.Owner;
        if (card is IEquipmentEffect effect) {
            for (int i = 0; i < effectApplicationCount; i++) {
                await effect.OnUnequipped(choiceContext, target);
            }
        }

        if (!CombatManager.Instance.IsOverOrEnding
            && (card.Pile?.Type == Entry.EquipPile
                || card.Pile?.Type == PileType.Play)) {
            await CardPileCmd.Add(
                card,
                PileType.Discard.GetPile(card.Owner));
        }

        if (removePower
            && target.GetPowerInstances<EquipmentPower>().Contains(power)) {
            await PowerCmd.Remove(power);
        }
    }

    private static async Task<bool> Attach(
        PlayerChoiceContext choiceContext,
        CardModel card,
        Creature? target,
        bool moveToEquipPile) {
        if (CombatManager.Instance.IsOverOrEnding
            || !CanEquip(card, target)) {
            return false;
        }

        CardPile? originalPile = card.Pile;
        if (moveToEquipPile) {
            CardPileAddResult result = await CardPileCmd.Add(
                card,
                Entry.EquipPile.GetPile(card.Owner),
                skipVisuals: false);
            if (!result.success) {
                return false;
            }
        }

        EquipmentPower power =
            (EquipmentPower)ModelDb.Power<EquipmentPower>().ToMutable();
        if (!power.Initialize(card)) {
            await RollBackPileMove(card, originalPile, moveToEquipPile);
            return false;
        }

        await PowerCmd.Apply(
            choiceContext,
            power,
            target!,
            1m,
            card.Owner.Creature,
            card);
        if (!target!.GetPowerInstances<EquipmentPower>().Contains(power)) {
            power.TakeEquipmentCard(out _);
            await RollBackPileMove(card, originalPile, moveToEquipPile);
            return false;
        }

        await MonsterCardVfx.PlayEquipCardFly(card, target!);

        if (card is IEquipmentEffect effect) {
            await effect.OnEquipped(choiceContext, target!);
            if (!power.RecordEffectApplication(card)) {
                await effect.OnUnequipped(choiceContext, target!);
                await RollBackPileMove(card, originalPile, moveToEquipPile);
                return false;
            }
        }

        return true;
    }

    private static bool IsValidTarget(
        CardModel card,
        Creature? target) {
        return target is { IsAlive: true }
            && target.PetOwner == card.Owner
            && target.HasPower<YgoPower>();
    }

    private static async Task RollBackPileMove(
        CardModel card,
        CardPile? originalPile,
        bool movedToEquipPile) {
        if (!movedToEquipPile
            || originalPile == null
            || card.Pile?.Type != Entry.EquipPile) {
            return;
        }

        await CardPileCmd.Add(
            card,
            originalPile,
            skipVisuals: false);
    }
}
