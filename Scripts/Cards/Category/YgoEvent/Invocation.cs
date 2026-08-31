using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.YgoEvent;

[RegisterCard(typeof(YgoEventCardPool))]
public class Invocation() : BaseSpellCard(1, CardType.Skill, CardRarity.Event, TargetType.None) {
    public override int CardId => 74063034;

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
            _ => PileType.Exhaust
        );

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await SummonUtil.ExecuteFusionSummon(new FusionSummonRequest(
            SourceCard: this,
            Owner: Owner,
            ChoiceContext: choiceContext,
            SelectionPrompt: SelectionScreenPrompt,
            GetAvailableMaterials: _ =>
                SummonUtil.GetFieldAndMonsterMaterialsFromPiles(
                    Owner,
                    [PileType.Discard]),
            GetMaterialDestination: _ => PileType.Exhaust
        ));
    }
}
