using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.Interactions.RightClick;
using VYgo.Core;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Common;

public abstract class BaseFiendsmithEquipLinkCard()
    : BaseExtraLinkCard(-1, CardRarity.Event, TargetType.None),
        IModRightClickableCard,
        IEquipmentEffect {
    protected virtual int EquipAttack => 0;
    protected virtual int EquipLife => 0;
    protected abstract string GraveyardSelectionPromptKey { get; }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.GraveyardAction(),
        YgoHoverTipConst.Equip(),
        YgoHoverTipConst.Enhance()
    ];

    public bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return context.PlayerChoiceContext != null
            && context.Player == Owner
            && Pile?.Type == PileType.Discard
            && Owner.Creature.Pets.Any(FiendsmithUtil.IsLightFiendMonster);
    }

    public async Task OnRightClick(ModRightClickExecutionContext context) {
        if (!CanExecuteRightClick(context)
            || context.PlayerChoiceContext is not { } choiceContext) {
            return;
        }

        NCapstoneContainer.Instance?.Close();
        Dictionary<CardModel, Creature> targets = Owner.Creature.Pets
            .Where(FiendsmithUtil.IsLightFiendMonster)
            .Where(creature => creature.Monster is BaseMonster { SourceCard: not null })
            .ToUniqueSourceCardTargets(GetType().Name);
        if (targets.Count == 0) return;

        CardModel? selected = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                Entry.MonsterPile.GetPile(Owner),
                Owner,
                new CardSelectorPrefs(
                    new LocString("cards", GraveyardSelectionPromptKey),
                    1),
                targets.ContainsKey))
            .FirstOrDefault();
        if (selected != null && targets.TryGetValue(selected, out Creature? target)) {
            await EquipCmd.EquipFromPile(choiceContext, this, target);
        }
    }

    async Task IEquipmentEffect.OnEquipped(
        PlayerChoiceContext choiceContext,
        Creature target
    ) {
        if (EquipAttack > 0) {
            await PowerCmd.Apply<AttackPower>(
                choiceContext,
                target,
                EquipAttack,
                Owner.Creature,
                this);
        }
        if (EquipLife > 0) {
            await MinionUtil.AddHp(target, EquipLife);
        }
    }

    async Task IEquipmentEffect.OnUnequipped(
        PlayerChoiceContext choiceContext,
        Creature target
    ) {
        if (!target.IsAlive) return;

        if (EquipAttack > 0 && target.GetPower<AttackPower>() is { } attackPower) {
            await PowerCmd.ModifyAmount(
                choiceContext,
                attackPower,
                -EquipAttack,
                Owner.Creature,
                this);
        }
        if (EquipLife > 0) {
            await CreatureCmd.LoseMaxHp(choiceContext, target, EquipLife, true);
        }
    }
}
