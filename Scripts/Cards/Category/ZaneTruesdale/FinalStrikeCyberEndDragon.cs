using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using VYgo.Core;
using VYgo.Scripts.Cards.Category.Machine;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class FinalStrikeCyberEndDragon() : BaseExtraFusionCard(-1, CardType.Attack, CardRarity.Rare, TargetType.None) {
    public override int CardId => 30275298;

    public override int BaseAttackVar => 20;
    public override int BaseLifeVar => 15;
    public override int UpgradeAttackVar => 5;

    // 机械族怪兽×3
    public override int FusionMaterialCount => 3;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [
        YgoKeywords.Piercing
    ];

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new DynamicVar("LifeLoss", 4)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.EnterField(),
        HoverTipFactory.FromCard<LimiterRemoval>()
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return material.CoreCard?.IsRace(YgoRace.Machine) == true;
    }

    protected override async Task AfterFusionSummoned(SummonPostPlayContext context) {
        // 若融合素材均为光属性，则无需失去生命
        bool allLightMaterials = context.Materials.Count > 0
            && context.Materials.All(material => material.CoreCard?.Attribute == "光");
        if (!allLightMaterials) {
            await CreatureCmd.Damage(
                context.ChoiceContext,
                context.Owner.Creature,
                DynamicVars["LifeLoss"].BaseValue,
                ValueProp.Unblockable | ValueProp.Unpowered,
                null,
                null);
        }

        CardModel limiterRemoval = context.Owner.Creature.CombatState
            .CreateCard<LimiterRemoval>(context.Owner);
        await CardPileCmd.AddGeneratedCardToCombat(
            limiterRemoval,
            PileType.Hand,
            context.Owner);
    }
}
