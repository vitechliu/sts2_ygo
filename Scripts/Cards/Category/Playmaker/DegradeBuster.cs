using MegaCrit.Sts2.Core.Localization;
using STS2RitsuLib.Interop.AutoRegistration;
using STS2RitsuLib.Interactions.RightClick;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Utils;
namespace VYgo.Scripts.Cards.Category.Playmaker;
[RegisterCard(typeof(PlaymakerCardPool))]
public class DegradeBuster() : BaseRightClickableMonsterCard(3, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 50426119;
    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 10;
    protected override RightClickType ClickType => RightClickType.Hand;
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Concat([new DynamicVar("Banish", 15)]);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [BaseSummonHoverTip, YgoHoverTipConst.HandAction(), YgoHoverTipConst.SpecialSummon(), HoverTipFactory.FromKeyword(CardKeyword.Exhaust)];
    protected override void OnUpgrade() => DynamicVars["Banish"].UpgradeValueBy(5);
    private static bool IsMaterial(CardModel card) => card is BaseMonsterCard monster && monster.YgoGetCore().IsRace(YgoRace.Cyberse);
    protected override LocString? ValidateRightClick(ModRightClickExecutionContext context) =>
        base.ValidateRightClick(context)
        ?? (Owner.MinionCount() >= Owner.GetMaxMinionCount() ? RightClickError("CAPACITY") : null)
        ?? (PileType.Discard.GetPile(Owner).Cards.Count(IsMaterial) < 2 ? RightClickError("CYBERSE_MATERIALS") : null);


    protected override async Task OnYgoRightClick(ModRightClickExecutionContext context) {
        if (context.PlayerChoiceContext is not { } choiceContext) return;
        var selected = (await CardSelectCmd.FromCombatPile(choiceContext, PileType.Discard.GetPile(Owner), Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 2), IsMaterial)).ToList();
        if (selected.Count != 2 || selected.Any(card => card.Pile?.Type != PileType.Discard)) return;
        foreach (var card in selected) await CardCmd.Exhaust(choiceContext, card);
        if (Pile?.Type == PileType.Hand && Owner.MinionCount() < Owner.GetMaxMinionCount()) await CardCmd.AutoPlay(choiceContext, this, null);
    }
}
