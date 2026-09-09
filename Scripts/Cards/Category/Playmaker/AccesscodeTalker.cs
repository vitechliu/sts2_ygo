using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Powers;
namespace VYgo.Scripts.Cards.Category.Playmaker;
[RegisterCard(typeof(PlaymakerCardPool))]
public class AccesscodeTalker() : BaseExtraLinkCard(-1, CardType.Skill, CardRarity.Rare, TargetType.None) {
    public override int CardId => 86066372;
    public override int BaseAttackVar => 15;
    public override int BaseLifeVar => 10;
    public override int UpgradeAttackVar => 5;
    protected override IEnumerable<DynamicVar> CanonicalVars => base.CanonicalVars.Concat([new DynamicVar("Boost", 5)]);
    public override int GetLinkMaterialCount(CoreCard card) => 2;
    public override bool CanUseLinkMaterial(SummonMaterial material) => material.IsEffectMonster;
    public override async Task AfterLinkSummoned(SummonPostPlayContext context) {
        int maximum = context.Materials.Select(material => material.CoreCard?.LinkCount ?? 0).DefaultIfEmpty().Max();
        if (maximum > 0 && context.SummonedCreature.IsAlive)
            await PowerCmd.Apply<AttackPower>(context.ChoiceContext, context.SummonedCreature,
                maximum * DynamicVars["Boost"].IntValue, Owner.Creature, this);
    }
}
