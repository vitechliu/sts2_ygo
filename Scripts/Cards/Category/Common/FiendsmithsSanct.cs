using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(EventCardPool))]
public class FiendsmithsSanct() : BaseSpellCard(0, CardType.Skill, CardRarity.Event, TargetType.None) {
    public override int CardId => 35552985;

    public override IEnumerable<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [
        HoverTipFactory.FromCard<FiendsmithsSanctToken>(),
        HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
        YgoHoverTipConst.SpecialSummon()
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        if (Owner.MinionCount() >= MinionUtil.MaxMinionCount) return;

        CardModel token = CombatState.CreateCard<FiendsmithsSanctToken>(Owner);
        await CardPileCmd.AddGeneratedCardToCombat(token, PileType.Play, Owner);
        await CardCmd.AutoPlay(choiceContext, token, null);
    }

    protected override void OnUpgrade() {
        RemoveKeyword(CardKeyword.Exhaust);
    }
}
