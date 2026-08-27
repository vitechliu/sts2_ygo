using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using VYgo.Core;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberdarknessDragon() : BaseExtraFusionCard(-1, CardType.Attack, CardRarity.Token, TargetType.None) {
    public override int CardId => 18967507;

    public override int BaseAttackVar => 20;
    public override int BaseLifeVar => 20;
    public override int UpgradeAttackVar => 10;

    // 「电子暗黑」怪兽×5
    public override int FusionMaterialCount => 5;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new PowerVar<NegatingPower>(1m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.Equip(),
        HoverTipFactory.FromPower<NegatingPower>()
    ];

    public override bool CanUseFusionMaterial(SummonMaterial material) {
        return material.VYgoCard?.ContainArchetype(YgoArchetypes.Cyberdark) == true;
    }

    protected override async Task AfterFusionSummoned(SummonPostPlayContext context) {
        // 从弃牌堆选择一只怪兽装备，并获得其攻击力加成
        BaseMonsterCard? selectedMonster = (await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(SelectionScreenPrompt, 1),
                context: context.ChoiceContext,
                pile: PileType.Discard.GetPile(context.Owner),
                player: context.Owner,
                filter: card => card is BaseMonsterCard))
            .OfType<BaseMonsterCard>()
            .FirstOrDefault();
        if (selectedMonster != null
            && await EquipCmd.EquipFromPile(
                context.ChoiceContext,
                selectedMonster,
                context.SummonedCreature)
            && selectedMonster.Attack > 0) {
            await PowerCmd.Apply<AttackPower>(
                context.ChoiceContext,
                context.SummonedCreature,
                selectedMonster.Attack,
                context.SummonedCreature,
                selectedMonster);
        }

        // 回合结束时：获得1层无效
        await PowerCmd.Apply<CyberdarknessDragonPower>(
            context.ChoiceContext,
            context.SummonedCreature,
            DynamicVars["NegatingPower"].BaseValue,
            context.Owner.Creature,
            this);
    }
}
