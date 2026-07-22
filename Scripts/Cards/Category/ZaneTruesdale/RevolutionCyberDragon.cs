using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Minion;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts.Var;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class RevolutionCyberDragon() : BaseMonsterCard(2, CardRarity.Rare, TargetType.None) {
    public override int CardId => 66664203;

    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 3;

    protected override bool UseAncient => true;
    protected override int PortraitCardId => 66664204;

    public int EnterDamage => DynamicVars["Damage"].IntValue;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar),
        new DamageVar(5m, ValueProp.Move)
    ];

    protected override bool ShouldGlowGoldInternal => CanSpecialSummon;

    private bool CanSpecialSummon => Owner.Creature.Pets.All(
        creature => !creature.IsAlive || creature.Monster is not MinionModel
    );

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        BaseSummonHoverTip,
        YgoHoverTipConst.SpecialSummon(),
        YgoHoverTipConst.Action(),
        YgoHoverTipConst.EnterField(),
        YgoHoverTipConst.FusionSummon()
    ];

    public override Task AfterCardDrawn(
        PlayerChoiceContext choiceContext,
        CardModel card,
        bool fromHandDraw
    ) {
        if (card == this) RefreshSpecialSummonCost();
        return Task.CompletedTask;
    }

    public override Task AfterCardEnteredCombat(CardModel card) {
        if (card == this) RefreshSpecialSummonCost();
        return Task.CompletedTask;
    }

    public override Task AfterCreatureAddedToCombat(Creature creature) {
        if (creature.PetOwner == Owner) RefreshSpecialSummonCost();
        return Task.CompletedTask;
    }

    public override Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength
    ) {
        if (!wasRemovalPrevented && creature.PetOwner == Owner) {
            RefreshSpecialSummonCost();
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        if (cardPlay.Card.Owner == Owner) RefreshSpecialSummonCost();
        return Task.CompletedTask;
    }

    protected override void OnUpgrade() {
        base.OnUpgrade();
        DynamicVars["Damage"].UpgradeValueBy(2m);
    }

    private void RefreshSpecialSummonCost() {
        EnergyCost.SetUntilPlayed(CanSpecialSummon ? 0 : CanonicalEnergyCost);
    }
    
    
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://VYgo/images/cards/{PortraitCardId}.png",
        FramePath: YgoFramePath,
        VisualStyle: CardVisualStyle.Ancient,
        AncientBannerPath:"res://VYgo/images/frame/ancient_banner_rare.png"
    );
}
