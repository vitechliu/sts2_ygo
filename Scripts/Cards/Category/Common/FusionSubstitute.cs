using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class FusionSubstitute()
    : BaseVYgoCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    protected override YgoType CardYgoType => YgoType.spell;

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        return SummonUtil.ExecuteFusionSummon(new FusionSummonRequest(
            SourceCard: this,
            Owner: Owner,
            ChoiceContext: choiceContext,
            SelectionPrompt: SelectionScreenPrompt,
            SelectMaterials: fusionCard => SummonUtil.SelectFieldMonsterMaterials(Owner, fusionCard.FusionMaterialCount)
        ));
    }

    public override int CardId => 74335036;
}
