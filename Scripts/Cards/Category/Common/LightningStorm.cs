using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;
using VYgo.Core;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(CommonCardPool))]
public class LightningStorm() : BaseSpellCard(1, CardType.Attack, CardRarity.Rare, TargetType.AllEnemies) {
    public override int CardId => 14532163;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(20m, ValueProp.Move),
        new PowerVar<WeakPower>(3m)
    ];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromPower<WeakPower>()
    ];

    private bool HasNoMonsters => Owner.MinionCount() == 0;

    protected override bool IsPlayable => base.IsPlayable && HasNoMonsters;

    protected override bool ShouldGlowRedInternal => !HasNoMonsters;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        if (!HasNoMonsters) return;

        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .TargetingAllOpponents(CombatState)
            .WithHitFx("vfx/vfx_attack_blunt")
            .Execute(choiceContext);
        await PowerCmd.Apply<WeakPower>(
            choiceContext,
            CombatState?.HittableEnemies,
            DynamicVars.Weak.BaseValue,
            Owner.Creature,
            this);
    }

    protected override void OnUpgrade() {
        DynamicVars.Damage.UpgradeValueBy(3m);
    }
}
