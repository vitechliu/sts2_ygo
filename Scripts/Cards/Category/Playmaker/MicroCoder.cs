using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Factories;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.Playmaker;

[RegisterCard(typeof(PlaymakerCardPool))]
public class MicroCoder() : BaseMonsterCard(1, CardType.Skill, CardRarity.Rare, TargetType.None), ILinkMaterialCard {
    public override int CardId => 2347477;

    public override int BaseAttackVar => 1;
    public override int BaseLifeVar => 1;
    public bool CanUseFromHand(BaseExtraLinkCard target) => target.ContainArchetype(YgoArchetypes.CodeTalker);

    public async Task AfterUsedAsLinkMaterial(PlayerChoiceContext choiceContext, Player owner, BaseExtraLinkCard target) {
        if (Owner != owner || Pile?.Type != PileType.Discard || !CanUseFromHand(target)) return;

        var choices = CardFactory.GetDistinctForCombat(owner,
            ModelDb.AllCards.OfType<BaseVYgoCard>().Where(card =>
                card is BaseSpellCard or BaseTrapCard && card.ContainArchetype(YgoArchetypes.Cynet)),
            3, owner.RunState.Rng.CombatCardGeneration).ToList();
        if (choices.Count == 0) return;
        if (IsUpgraded) CardCmd.Upgrade(choices, CardPreviewStyle.HorizontalLayout);
        var selected = await CardSelectCmd.FromChooseACardScreen(choiceContext, choices, owner, canSkip: false);
        if (selected != null) await CardPileCmd.AddGeneratedCardToCombat(selected, PileType.Hand, owner);
    }
}
