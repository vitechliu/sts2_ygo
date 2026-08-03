using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberdarkRealm() : BaseSpellCard(2, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    public const int CostReduction = 1;

    public override int CardId => 64753988;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new EnergyVar("CostReduction", CostReduction)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        CardModel? selectedCard = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: PileType.Draw.GetPile(Owner),
                player: Owner,
                filter: model => model is BaseMonsterCard monsterCard && monsterCard.ContainArchetype(YgoArchetypes.Cyberdark)))
            .FirstOrDefault();
        if (selectedCard != null) {
            await CardPileCmd.Add(selectedCard, PileType.Hand);
        }
        await PowerCmd.Apply<CyberdarkRealmPower>(choiceContext, Owner.Creature, 1m, Owner.Creature, this);
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
    }
}
