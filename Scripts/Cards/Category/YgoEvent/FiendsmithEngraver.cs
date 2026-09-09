using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.Capstones;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Scaffolding.Characters;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(YgoEventCardPool))]
public class FiendsmithEngraver()
    : BaseRightClickableMonsterCard(2, CardType.Skill, CardRarity.Event, TargetType.None) {
    public override int CardId => 60764609;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 7;

    protected override RightClickType ClickType =>
        Pile?.Type == PileType.Discard ? RightClickType.Graveyard : RightClickType.Hand;
    protected override int RightClickCost => ClickType == RightClickType.Hand ? base.RightClickCost : 0;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.HandAction(),
        YgoHoverTipConst.GraveyardAction(),
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override LocString? ValidateRightClick(ModRightClickExecutionContext context) {
        LocString? error = base.ValidateRightClick(context);
        if (error != null) return error;
        if (Pile?.Type == PileType.Hand) {
            return PileType.Draw.GetPile(Owner).Cards.Any(FiendsmithUtil.IsFiendsmithSpellTrap)
                ? null : RightClickError("NO_SEARCH_TARGET");
        }
        if (Owner.MinionCount() >= Owner.GetMaxMinionCount()) return RightClickError("CAPACITY");
        if (Owner.Creature.HasPower<FiendsmithEngraverUsedThisTurnPower>()) return RightClickError("ONCE_PER_TURN");
        return PileType.Discard.GetPile(Owner).Cards.Any(card =>
            card != this && FiendsmithUtil.IsLightFiendMonster(card))
            ? null : RightClickError("LIGHT_FIEND_MATERIAL");
    }

    protected override async Task OnYgoRightClick(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choiceContext) return;

        NCapstoneContainer.Instance?.Close();
        if (Pile?.Type == PileType.Hand) {
            await CardCmd.Discard(choiceContext, this);

            CardModel? selected = (await CardSelectCmd.FromCombatPile(
                    choiceContext,
                    PileType.Draw.GetPile(Owner),
                    Owner,
                    new CardSelectorPrefs(
                        new LocString("cards", "V_YGO_CARD_FIENDSMITH_ENGRAVER.handSelectionScreenPrompt"),
                        1),
                    FiendsmithUtil.IsFiendsmithSpellTrap))
                .FirstOrDefault();
            if (selected != null) {
                await CardPileCmd.Add(selected, PileType.Hand);
            }
            return;
        }

        if (Pile?.Type != PileType.Discard) return;

        BaseMonsterCard? returnedMonster = (await CardSelectCmd.FromCombatPile(
                choiceContext,
                PileType.Discard.GetPile(Owner),
                Owner,
                new CardSelectorPrefs(
                    new LocString("cards", "V_YGO_CARD_FIENDSMITH_ENGRAVER.graveyardSelectionScreenPrompt"),
                    1),
                card => card != this && FiendsmithUtil.IsLightFiendMonster(card)))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (returnedMonster == null || Owner.MinionCount() >= Owner.GetMaxMinionCount()) return;

        await CardPileCmd.Add(
            returnedMonster,
            returnedMonster.IsExtra ? Entry.ExtraPile : PileType.Draw);
        Creature? summoned = await AutoPlayAndCaptureSummonedCreature(choiceContext, null);
        if (summoned != null) {
            await PowerCmd.Apply<FiendsmithEngraverUsedThisTurnPower>(
                choiceContext,
                Owner.Creature,
                1m,
                summoned,
                this,
                true);
        }
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        EnergyCost.UpgradeBy(-1);
    }
}
