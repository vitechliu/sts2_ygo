using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
[RegisterCharacterStarterCard(typeof(PlaymakerCharacter), 1)]
public class Draconnet() : BaseMonsterCard(1, CardRarity.Basic, TargetType.None) {
    public override int CardId => 62706865;

    public override int BaseAttackVar => 4;
    public override int BaseLifeVar => 3;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.SpecialSummon()
    ];
}
