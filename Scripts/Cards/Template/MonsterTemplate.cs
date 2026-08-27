using MegaCrit.Sts2.Core.Entities.Cards;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Template;

// [RegisterCard(typeof(ZaneTruesdaleCardPool))] //需要选择卡牌Pool
public abstract class MonsterTemplate() : BaseMonsterCard(1, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => -1;

    public override int BaseAttackVar => 1;
    public override int BaseLifeVar => 1;
    public override int UpgradeAttackVar => 1;
    public override int UpgradeLifeVar => 1;

    // protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
    //     BaseSummonHoverTip,
    // ];
}
