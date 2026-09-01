using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Characters;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Link;

[RegisterCard(typeof(LinkCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter))]
public class SPLittleKnight() : BaseExtraLinkCard(energyCost, CardType.Skill, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 29301450;

    public int BanishAmount => DynamicVars["Banish"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new DynamicVar("Banish", 10m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.Banish()
    ];
    
    private const int energyCost = -1;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;

    public override int BaseAttackVar => 5;
    public override int BaseLifeVar => 5;

    public override int GetLinkMaterialCount(CoreCard coreCard) => 2;

    public override bool CanUseLinkMaterial(SummonMaterial material) {
        return material.IsEffectMonster;
    }

    protected override void OnUpgrade() {
        DynamicVars["Banish"].UpgradeValueBy(3m);
    }
}
