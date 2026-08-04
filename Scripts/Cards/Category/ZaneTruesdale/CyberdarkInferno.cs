using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberdarkInferno() : BaseSpellCard(1, CardType.Power, CardRarity.Token, TargetType.None) {
    public override int CardId => 44352516;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new LifeVar(2)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<CyberdarkInfernoPower>(),
        YgoHoverTipConst.Enhance(),
        YgoHoverTipConst.PowerAction(),
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await PowerCmd.Apply<CyberdarkInfernoPower>(
            choiceContext,
            Owner.Creature,
            DynamicVars["Life"].BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() {
        DynamicVars["Life"].UpgradeValueBy(1m);
    }
}
