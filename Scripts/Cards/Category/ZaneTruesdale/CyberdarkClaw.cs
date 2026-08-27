using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;
using VYgo.Core;

namespace VYgo.Scripts.Cards.Category.ZaneTruesdale;

[RegisterCard(typeof(ZaneTruesdaleCardPool))]
public class CyberdarkClaw()
    : BaseCyberdarkHandActionMonsterCard(1, CardType.Attack, CardRarity.Common, TargetType.None) {
    public override int CardId => 82562802;

    public override int BaseAttackVar => 4;
    public override int BaseLifeVar => 2;

    protected override IEnumerable<CardModel> GetHandActionCandidates() {
        return ModelDb.AllCards
            .OfType<BaseVYgoCard>()
            .Where(card =>
                (card.YgoCardType is YgoType.spell or YgoType.trap)
                && card.ContainArchetype(YgoArchetypes.Cyberdark));
    }
}
