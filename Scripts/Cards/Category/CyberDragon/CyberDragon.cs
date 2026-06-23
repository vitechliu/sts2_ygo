using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Combat.History.Entries;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using MegaCrit.Sts2.Core.Models;
using MinionLib.Commands;
using MinionLib.Minion;
using STS2RitsuLib.Interop.AutoRegistration;
using VYgo.Scripts.Monsters.YGO;
using VYgo.Scripts.Pools;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards.Category.CyberDragon;

[RegisterCard(typeof(RedhatCardPool))]
[RegisterCharacterStarterCard(typeof(RedhatCharacter), 3)]
public class CyberDragon() : BaseMonsterCard(energyCost, rarity, targetType, shouldShowInCardLibrary) {
    public override int CardId => 70095154;

    private const int energyCost = 2;
    private const CardRarity rarity = CardRarity.Common;
    private const TargetType targetType = TargetType.None;
    private const bool shouldShowInCardLibrary = true;


    // protected override IEnumerable<IHoverTip> ExtraHoverTips => new List<IHoverTip>();{
    //     HoverTipFactory.FromKeyword(CardKeyword.Exhaust),
    // }

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(7),
        new LifeVar(5)
    ];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        _ = await MinionUtil.AddMinionInstant<CyberDragonMinion>(choiceContext, Owner, new MinionSummonOptions(
            MaxHp: Life, // 血量
            PrimaryStatAmount: Attack, // 主要参数（具体内容在随从的 OnSummon 里定义），还有次要参数等可以按需传入
            Source: this, // 召唤来源（通常是这张牌）
            Position: MinionPosition.Front)
        );
    }

    protected override void OnUpgrade() {
        DynamicVars["Life"].UpgradeValueBy(1);
        DynamicVars["Attack"].UpgradeValueBy(1);
    }

    protected override bool ShouldGlowGoldInternal => Active;
    
    bool Active => Owner.MinionCount() <= 0;

    void FlushCost() {
        EnergyCost.SetUntilPlayed(Active ? 0 : 1);
    }
    public override Task AfterCardEnteredCombat(CardModel card) {
        if (card != this || this.IsClone)
            return Task.CompletedTask;
        FlushCost();
        return Task.CompletedTask;
    }

    public override Task AfterCardPlayed(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        if (cardPlay.Card.Owner != this.Owner) return Task.CompletedTask;
        FlushCost();
        return Task.CompletedTask;
    }
}