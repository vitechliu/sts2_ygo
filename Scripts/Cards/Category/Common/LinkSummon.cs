using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(LinkCardPool))]
[RegisterCharacterStarterCard(typeof(ZaneTruesdaleCharacter), 1)]
public class LinkSummon() : BaseSummonCard(0, CardType.Skill, CardRarity.Basic, TargetType.None) {
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.FusionSummon()
    ];
    protected override Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        return SummonUtil.ExecuteLinkSummon(new LinkSummonRequest(
            SourceCard: this,
            Owner: Owner,
            ChoiceContext: choiceContext,
            SelectionPrompt: SelectionScreenPrompt,
            GetAvailableMaterials: GetAvailableLinkMaterials
        ));
    }

    private IReadOnlyList<SummonMaterial> GetAvailableLinkMaterials(BaseExtraLinkCard linkCard, CoreCard coreCard) {
        return SummonUtil.GetFieldMonsterMaterials(Owner);
    }
}
