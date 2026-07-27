using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class PhotonGeneratorUnit()
    : BaseSpellCard(0, CardType.Skill, CardRarity.Uncommon, TargetType.None) {
    public override int CardId => 66607691;

    protected override bool ShouldGlowRedInternal => !HasCyberDragonTribute;

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<CyberLaserDragon>(IsUpgraded),
        YgoHoverTipConst.SummonNormal()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        if (!HasCyberDragonTribute) return;

        bool tributeSucceeded = await SummonUtil.ExecuteFieldTribute(
            choiceContext,
            Owner,
            ModelDb.Card<CyberLaserDragon>(),
            1,
            IsCyberDragonTribute
        );
        if (!tributeSucceeded || Owner.Creature.IsDead) return;

        CardModel cyberLaserDragon = CombatState.CreateCard<CyberLaserDragon>(Owner);
        if (IsUpgraded) {
            CardCmd.Upgrade(cyberLaserDragon);
        }
        await CardPileCmd.AddGeneratedCardToCombat(
            cyberLaserDragon,
            PileType.Play,
            Owner
        );
        await CardCmd.AutoPlay(choiceContext, cyberLaserDragon, null);
    }

    private bool HasCyberDragonTribute =>
        SummonUtil.HasValidFieldTribute(Owner, 1, IsCyberDragonTribute);

    private static bool IsCyberDragonTribute(SummonMaterial material) {
        return material.VYgoCard?.ContainArchetype(YgoArchetypes.CyberDragon) == true;
    }
}
