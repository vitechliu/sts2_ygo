using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberloadFusion()
    : BaseSpellCard(1, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 55704856;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        CardKeyword.Retain
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.FusionSummon()
    ];

    protected override bool IsPlayable => base.IsPlayable
        && SummonUtil.HasFusionSummonTarget(
            Owner,
            _ => SummonUtil.GetFieldAndMonsterMaterialsFromPiles(
                Owner,
                [PileType.Discard]
            ),
            _ => PileType.Draw,
            IsMachineFusionMonster
        );

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
            GetMaterialDestination: _ => PileType.Draw,
            FusionCardFilter: IsMachineFusionMonster
        ));
    }

    protected override void OnUpgrade() {
        EnergyCost.UpgradeBy(-1);
    }

    private static bool IsMachineFusionMonster(BaseExtraFusionCard card) {
        return card.YgoGetCore().IsRace(YgoRace.Machine);
    }
}
