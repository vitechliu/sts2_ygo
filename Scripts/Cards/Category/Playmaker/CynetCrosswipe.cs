using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.ValueProps;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class CynetCrosswipe() : BaseSpellCard(1, CardType.Attack, CardRarity.Common, TargetType.AnyEnemy) {
    public override int CardId => 77449773;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new DamageVar(8m, ValueProp.Move)
    ];

    private bool HasCyberseMonsterOnField => Owner.Creature.Pets.Any(
        pet => pet.Monster is BaseMonster monster && monster.YgoGetCore().IsRace(YgoRace.Cyberse)
    );

    protected override bool IsPlayable => base.IsPlayable && HasCyberseMonsterOnField;

    protected override bool ShouldGlowRedInternal => !HasCyberseMonsterOnField;

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        if (!HasCyberseMonsterOnField) return;
        ArgumentNullException.ThrowIfNull(cardPlay.Target);
        await DamageCmd.Attack(DynamicVars.Damage.BaseValue)
            .FromCard(this, cardPlay)
            .Targeting(cardPlay.Target)
            .WithHitFx("vfx/vfx_attack_slash")
            .Execute(choiceContext);
    }

    protected override void OnUpgrade() {
        DynamicVars.Damage.UpgradeValueBy(2m);
    }
}
