using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberdarkDragon() : BaseExtraFusionCard(-1, CardRarity.Token, TargetType.None) {
    public override int CardId => 40418351;

    public override int BaseAttackVar => 10;
    public override int BaseLifeVar => 10;
    public override int UpgradeAttackVar => 5;

    // 「电子暗黑」怪兽×3
    public override int FusionMaterialCount => 3;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new AttackVar("BoostAttack", 5)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        ..base.AdditionalHoverTips,
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.Equip(),
        YgoHoverTipConst.Enhance()
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

        // 墓地每有一只怪兽，获得强化X/0
        int graveMonsterCount = PileType.Discard.GetPile(context.Owner).Cards
            .Count(card => card is BaseMonsterCard);
        int boostPerMonster = DynamicVars["BoostAttack"].IntValue;
        if (graveMonsterCount > 0 && boostPerMonster > 0) {
            await PowerCmd.Apply<AttackPower>(
                context.ChoiceContext,
                context.SummonedCreature,
                boostPerMonster * graveMonsterCount,
                context.Owner.Creature,
                this);
        }
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["BoostAttack"].UpgradeValueBy(1);
    }
}
