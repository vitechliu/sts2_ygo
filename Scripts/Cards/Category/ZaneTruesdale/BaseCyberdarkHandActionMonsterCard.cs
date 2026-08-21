using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using STS2RitsuLib.Interactions.RightClick;
using VYgo.Core;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

public abstract class BaseCyberdarkHandActionMonsterCard(
    int baseCost,
    CardRarity rarity,
    TargetType target)
    : BaseRightClickableMonsterCard(baseCost, rarity, target),
        IEquipmentEffect {
    protected virtual int EquipAttack => 3;
    protected virtual int EquipLife => 0;

    protected override RightClickType ClickType => RightClickType.Hand;
    
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("EquipAttack", EquipAttack),
        new LifeVar("EquipLife", EquipLife),
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.HandAction(),
        YgoHoverTipConst.Equip(),
        YgoHoverTipConst.Enhance(),
    ];

    public override bool CanExecuteRightClick(ModRightClickExecutionContext context) {
        return base.CanExecuteRightClick(context)
            && GetHandActionCandidates().Any();
    }

    protected override async Task OnYgoRightClick(ModRightClickExecutionContext context) {
        PlayerChoiceContext? choiceContext = context.PlayerChoiceContext;
        if (choiceContext == null) return;

        List<CardModel> choices = CardFactory.GetDistinctForCombat(
                Owner,
                GetHandActionCandidates(),
                3,
                Owner.RunState.Rng.CombatCardGeneration)
            .ToList();
        if (choices.Count == 0) return;

        if (IsUpgraded) {
            CardCmd.Upgrade(choices, CardPreviewStyle.HorizontalLayout);
        }

        await CardCmd.Discard(choiceContext, this);
        CardModel? selectedCard = await CardSelectCmd.FromChooseACardScreen(
            choiceContext,
            choices,
            Owner);
        if (selectedCard != null) {
            await CardPileCmd.AddGeneratedCardToCombat(
                selectedCard,
                GetGeneratedPileType(selectedCard),
                Owner);
        }
    }

    protected abstract IEnumerable<CardModel> GetHandActionCandidates();

    protected virtual PileType GetGeneratedPileType(CardModel selectedCard) {
        return PileType.Hand;
    }

    async Task IEquipmentEffect.OnEquipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        int attack = DynamicVars["EquipAttack"].IntValue;
        int life = DynamicVars["EquipLife"].IntValue;

        if (attack > 0) {
            await PowerCmd.Apply<AttackPower>(
                choiceContext,
                target,
                attack,
                Owner.Creature,
                this);
        }
        if (life > 0) {
            await MinionUtil.AddHp(target, life);
        }
    }

    async Task IEquipmentEffect.OnUnequipped(
        PlayerChoiceContext choiceContext,
        Creature target) {
        if (!target.IsAlive) return;

        int attack = DynamicVars["EquipAttack"].IntValue;
        int life = DynamicVars["EquipLife"].IntValue;
        if (attack > 0 && target.GetPower<AttackPower>() is { } attackPower) {
            await PowerCmd.ModifyAmount(
                choiceContext,
                attackPower,
                -attack,
                Owner.Creature,
                this);
        }
        if (life > 0) {
            await CreatureCmd.LoseMaxHp(
                choiceContext,
                target,
                life,
                true);
        }
    }
}
