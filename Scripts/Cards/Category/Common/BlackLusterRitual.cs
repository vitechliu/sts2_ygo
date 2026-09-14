using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class BlackLusterRitual() : BaseSpellCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None)
{
    public override int CardId => 55761792;
    protected override bool IsPlayable => PileType.Hand.GetPile(Owner).Cards.OfType<BlackLusterSoldier>().Any(c => RitualSummonCmd.BuildSelection(c).HasValidCombination);
    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Retain];
    protected override async Task OnPlay(PlayerChoiceContext context, CardPlay play)
    {
        await CreatureCmd.TriggerAnim(Owner.Creature, "Cast", Owner.Character.CastAnimDelay);
        var target = (await CardSelectCmd.FromCombatPile(context, PileType.Hand.GetPile(Owner), Owner,
            new CardSelectorPrefs(SelectionScreenPrompt, 1), filter: c => c is BlackLusterSoldier ritual && RitualSummonCmd.BuildSelection(ritual).HasValidCombination)).FirstOrDefault() as BlackLusterSoldier;
        if (target != null) await RitualSummonCmd.Execute(target, context, this);
    }
    protected override void OnUpgrade() => EnergyCost.UpgradeBy(-1);
}
