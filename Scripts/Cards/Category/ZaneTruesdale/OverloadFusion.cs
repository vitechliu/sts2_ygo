using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class OverloadFusion()
    : BaseSpellCard(1, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => 3659803;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Ethereal
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.FusionSummon(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];

    protected override async Task OnPlay(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay
    ) {
        await SummonUtil.ExecuteFusionSummon(new FusionSummonRequest(
            SourceCard: this,
            Owner: Owner,
            ChoiceContext: choiceContext,
            SelectionPrompt: SelectionScreenPrompt,
            GetAvailableMaterials: _ =>
                SummonUtil.GetFieldAndMonsterMaterialsFromPiles(
                    Owner,
                    [PileType.Discard]
                ),
            GetMaterialDestination: _ => PileType.Exhaust,
            FusionCardFilter: IsDarkMachineFusionMonster
        ));
    }

    protected override void OnUpgrade() {
        RemoveKeyword(CardKeyword.Ethereal);
    }

    private static bool IsDarkMachineFusionMonster(BaseExtraFusionCard card) {
        return card.YgoGetCore() is { Attribute: "暗" } coreCard
            && coreCard.IsRace(YgoRace.Machine);
    }
}
