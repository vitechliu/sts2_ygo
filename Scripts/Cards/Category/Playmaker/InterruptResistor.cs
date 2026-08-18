using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using VYgo.Core;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class InterruptResistor() : BaseMonsterCard(2, CardRarity.Common, TargetType.None) {
    public override int CardId => 2414168;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 15;
    public override int UpgradeLifeVar => 5;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon()
    ];

    public override async Task AfterDamageReceived(
        PlayerChoiceContext choiceContext,
        Creature target,
        DamageResult result,
        ValueProp props,
        Creature? dealer,
        CardModel? cardSource
    ) {
        if (!CombatManager.Instance.IsInProgress
            || Pile?.Type != PileType.Hand
            || target != Owner.Creature
            || result.UnblockedDamage <= 0
            || Owner.MinionCount() >= MinionUtil.MaxMinionCount) {
            return;
        }

        Creature? summoned = await AutoPlayAndCaptureSummonedCreature(
            choiceContext,
            null);
        if (summoned != null) {
            await PowerCmd.Apply<AttackPower>(
                choiceContext,
                summoned,
                result.UnblockedDamage,
                Owner.Creature,
                this);
        }
    }
}
