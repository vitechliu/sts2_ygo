using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interactions.RightClick;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberEltanin() : BaseRightClickableMonsterCard(1, CardType.Attack, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 33093439;
    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;
    protected override bool IsPlayable => false;
    protected override RightClickType ClickType => RightClickType.Hand;
    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(0), new LifeVar(1), new DynamicVar("EnterDamage", 10),
        new DynamicVar("Boost", 5)
    ];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip, YgoHoverTipConst.HandAction(), YgoHoverTipConst.SpecialSummon(),
        YgoHoverTipConst.Enhance(), HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    private static bool IsMaterial(CardModel card) => card is BaseMonsterCard monster
        && monster.YgoGetCore() is { Attribute: "光" } core && core.IsRace(YgoRace.Machine);

    protected override MegaCrit.Sts2.Core.Localization.LocString? ValidateRightClick(ModRightClickExecutionContext context) =>
        base.ValidateRightClick(context)
        ?? (Owner.MinionCount() >= Owner.GetMaxMinionCount() ? RightClickError("CAPACITY") : null)
        ?? (!PileType.Discard.GetPile(Owner).Cards.Any(IsMaterial) ? RightClickError("LIGHT_MACHINE_MATERIAL") : null);

    public override async Task OnRightClick(ModRightClickExecutionContext context) {
        if (!TryValidateRightClick(context) || context.PlayerChoiceContext is not { } choice) return;
        try {
            var selected = (await CardSelectCmd.FromCombatPile(choice,
                PileType.Discard.GetPile(Owner), Owner,
                new CardSelectorPrefs(SelectionScreenPrompt, 1, PileType.Discard.GetPile(Owner).Cards.Count),
                IsMaterial)).ToList();
            if (selected.Count == 0 || !TryValidateRightClick(context)) return;
            await SpendResources();
            int count = 0;
            foreach (var card in selected) {
                if (card.Pile?.Type != PileType.Discard || !IsMaterial(card)) continue;
                await CardCmd.Exhaust(choice, card);
                if (card.Pile?.Type == PileType.Exhaust) count++;
            }
            if (count == 0) return;
            var summoned = await AutoPlayAndCaptureSummonedCreature(choice, null);
            if (summoned?.IsAlive != true) return;
            await PowerCmd.Apply<AttackPower>(choice, summoned, count * DynamicVars["Boost"].IntValue, Owner.Creature, this);
            await MinionUtil.AddHp(summoned, count * DynamicVars["Boost"].IntValue);
        }
        finally { InvokeExecutionFinished(); }
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
        DynamicVars["EnterDamage"].UpgradeValueBy(5);
    }
}
