using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core.Effects;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards;

public abstract class BaseMonsterCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseVYgoCard(baseCost, CardType.Skill, rarity, target, showInCardLibrary) {
    protected List<IHoverTip> BaseSummonHoverTips => [YgoHoverTipConst.Summon(this)];
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => BaseSummonHoverTips;

    //最大随从数量限制
    protected override bool IsPlayable => Owner.MinionCount() < MinionUtil.MAX_MINION_COUNT;

    public virtual bool IsExtra => false;

    public virtual int BaseAttackVar => 1;
    public virtual int BaseLifeVar => 1;
    public virtual int UpgradeAttackVar => 1;
    public virtual int UpgradeLifeVar => 1;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar)
    ];

    protected override void OnUpgrade() {
        if (UpgradeAttackVar != 0) DynamicVars["Attack"].UpgradeValueBy(UpgradeAttackVar);
        if (UpgradeLifeVar != 0) DynamicVars["Life"].UpgradeValueBy(UpgradeLifeVar);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        var c = this.YgoGetMonster();
        if (c == null) return;
        Entry.Logger.Info("findMonster");
        var summonedCreature = await MinionUtil.AddMinionInstant(
            c.GetType(),
            choiceContext,
            Owner,
            new MinionSummonOptions(
                MaxHp: Life,
                PrimaryStatAmount: Attack,
                Source: this,
                Position: MinionPosition.Front
            )
        );
        await MonsterCardVfx.PlaySummonCardFly(this, summonedCreature);
    }

    //怪兽卡打出后和能力卡一样消失，只有怪兽死亡后才会移入弃牌堆
    protected override PileType GetResultPileTypeForCardPlay() {
        return PileType.None;
    }


    public int Life => DynamicVars["Life"].IntValue;
    public int Attack => DynamicVars["Attack"].IntValue;
}
