using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Commands.Builders;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using VYgo.Core;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class LockoutGardna() : BaseMonsterCard(1, CardType.Skill, CardRarity.Common, TargetType.None) {
    public override int CardId => 37310367;

    public override int BaseAttackVar => 3;
    public override int BaseLifeVar => 3;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon()
    ];

    public override async Task BeforeAttack(AttackCommand command) {
        if (Pile?.Type != PileType.Hand
            || command.Attacker is not { } attacker
            || attacker.Side == Owner.Creature.Side
            || Owner.MinionCount() >= Owner.GetMaxMinionCount()) {
            return;
        }

        var choiceContext = new BlockingPlayerChoiceContext();
        Creature? summoned = await AutoPlayAndCaptureSummonedCreature(
            choiceContext,
            null);
        if (summoned != null && IsUpgraded) {
            await PowerCmd.Apply<BattleDestructionProtectionPower>(
                choiceContext,
                summoned,
                1m,
                Owner.Creature,
                this,
                true);
        }
    }
}
