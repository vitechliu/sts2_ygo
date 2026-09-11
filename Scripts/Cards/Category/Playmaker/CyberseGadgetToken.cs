using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Pools;
using MegaCrit.Sts2.Core.Entities.Cards;
namespace VYgo.Scripts.Cards.Category.Playmaker;
[RegisterCard(typeof(PlaymakerCardPool))]
public class CyberseGadgetToken() : BaseTokenCard(TargetType.None) {
    public override int CardId => 645088;
    public override int BaseAttackVar => 0;
    public override int BaseLifeVar => 1;
}
