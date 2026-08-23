using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using VYgo.Core;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class SprightGammaBurst() : BaseSpellCard(1, CardType.Skill, CardRarity.Event, TargetType.None) {
    public override int CardId => 42431833;

    public int BoostAttack => DynamicVars["BoostAttack"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar("BoostAttack", 7)
    ];

    protected override void OnUpgrade() {
        DynamicVars["BoostAttack"].UpgradeValueBy(4m);
    }

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        YgoHoverTipConst.Enhance()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        var targets = Owner.Creature.Pets
            .Where(pet => pet.IsAlive
                && pet.Monster is BaseMonster monster
                && YgoSummonRules.IsLevel2Rank2OrLink2(monster.YgoGetCore()))
            .ToList();
        if (targets.Count == 0) return;

        await PowerCmd.Apply<AttackPower>(
            choiceContext,
            targets,
            BoostAttack,
            Owner.Creature,
            this);
    }
}
