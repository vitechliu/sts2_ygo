using MegaCrit.Sts2.Core.CardSelection;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Pools;

namespace VYgo.Scripts.Cards.Category.Common;

[RegisterCard(typeof(RedhatCardPool))]
// [RegisterCharacterStarterCard(typeof(RedhatCharacter), 1)]
public class LinkSummon() : BaseSummonCard(0, CardType.Skill, CardRarity.Basic, TargetType.None) {
    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        var pile = Entry.ExtraPile.GetPile(Owner);
        if (pile.Cards.Count <= 0) return;
        //todo linkSelect
        if ((await CardSelectCmd.FromCombatPile(
                prefs: new CardSelectorPrefs(base.SelectionScreenPrompt, 1),
                context: choiceContext,
                pile: pile, player: Owner))
            .FirstOrDefault() is not BaseExtraLinkCard cardModel) return;
        var coreCard = cardModel.YgoGetCore();
        if (coreCard is null) {
            Entry.Logger.Error("Failed to get core card: " + cardModel.CardId);
            return;
        }

        var materials = TrySelectLinkMaterials(coreCard);
        if (materials is null) return;
        
        //第一步，为素材播放选中动画
        
        
        //第二步，播放卡片动画
        
        //第三步，展开连接圆盘并播放圆盘动画
        
        //第四步，弹出链接目标并
    }
}