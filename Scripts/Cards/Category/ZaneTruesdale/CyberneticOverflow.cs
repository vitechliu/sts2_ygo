using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberneticOverflow() : BaseTrapCard(0, CardType.Power, CardRarity.Common, TargetType.None) {
    public override int CardId => 82428674;
    protected override IEnumerable<DynamicVar> CanonicalVars => [new DamageVar(10, ValueProp.Unpowered)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<CyberneticOverflowPower>(), YgoHoverTipConst.SetCard(),
        YgoHoverTipConst.PowerAction(), HoverTipFactory.FromKeyword(CardKeyword.Exhaust)
    ];
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await PowerCmd.Apply<CyberneticOverflowPower>(choiceContext, Owner.Creature,
            DynamicVars.Damage.BaseValue, Owner.Creature, this);
    }
    protected override void OnUpgrade() => DynamicVars.Damage.UpgradeValueBy(3);
}
