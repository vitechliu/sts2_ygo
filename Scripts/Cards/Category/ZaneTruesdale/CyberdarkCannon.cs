using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberdarkCannon()
    : BaseCyberdarkHandActionMonsterCard(1, CardType.Attack, CardRarity.Common, TargetType.None) {
    public override int CardId => 45078193;

    public override int BaseAttackVar => 4;
    public override int BaseLifeVar => 2;

    protected override IEnumerable<CardModel> GetHandActionCandidates() {
        return ModelDb.AllCards
            .OfType<BaseMonsterCard>()
            .Where(card => card.ContainArchetype(YgoArchetypes.Cyberdark));
    }

    protected override PileType GetGeneratedPileType(CardModel selectedCard) {
        return selectedCard is BaseMonsterCard { IsExtra: true }
            ? Entry.ExtraPile
            : PileType.Hand;
    }
}
