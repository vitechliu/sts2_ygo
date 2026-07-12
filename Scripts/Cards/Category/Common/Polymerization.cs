using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(FusionCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 2)]
public class Polymerization()
    : BaseSpellCard(0, CardType.Skill, CardRarity.Basic, TargetType.None) {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.FusionSummon()
    ];

    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        return SummonUtil.ExecuteFusionSummon(new FusionSummonRequest(
            SourceCard: this,
            Owner: Owner,
            ChoiceContext: choiceContext,
            SelectionPrompt: SelectionScreenPrompt,
            SelectMaterials: fusionCard => SummonUtil.SelectFieldAndHandMonsterMaterials(Owner, fusionCard.FusionMaterialCount)
        ));
    }

    public override int CardId => 24094653;
}
