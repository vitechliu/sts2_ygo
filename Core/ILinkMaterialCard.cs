using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using VYgo.Scripts.Cards;
namespace VYgo.Core;
/// <summary>由来源战斗卡统一处理手牌素材资格和连接素材送墓效果。</summary>
public interface ILinkMaterialCard {
    bool CanUseFromHand(BaseExtraLinkCard target);
    Task AfterUsedAsLinkMaterial(PlayerChoiceContext choiceContext, Player owner, BaseExtraLinkCard target);
}
