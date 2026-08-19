using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core.Effects;
using VYgo.Core.Hooks;
using MinionLib.Minion;
using VYgo.Core;
using VYgo.Core.Summon;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards;

public abstract class BaseMonsterCard(
    int baseCost,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseVYgoCard(baseCost, CardType.Skill, rarity, target, showInCardLibrary) {
    private Action<Creature>? _summonResultObserver;

    protected IHoverTip BaseSummonHoverTip => YgoHoverTipConst.Summon(this);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [BaseSummonHoverTip];

    //最大随从数量限制
    protected override bool IsPlayable => Owner.MinionCount() < MinionUtil.MaxMinionCount;

    public virtual bool IsExtra => false;

    public virtual bool ForceAsTuner => false;

    public virtual int BaseAttackVar => 1;
    public virtual int BaseLifeVar => 1;
    public virtual int UpgradeAttackVar => 0;
    public virtual int UpgradeLifeVar => 0;

    protected override IEnumerable<DynamicVar> CanonicalVars => [
        new AttackVar(BaseAttackVar),
        new LifeVar(BaseLifeVar)
    ];

    protected override void OnUpgrade() {
        if (UpgradeAttackVar != 0) DynamicVars["Attack"].UpgradeValueBy(UpgradeAttackVar);
        if (UpgradeLifeVar != 0) DynamicVars["Life"].UpgradeValueBy(UpgradeLifeVar);
    }

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay) {
        await SummonMonster(choiceContext, cardPlay, new SummonContext(IsSpecialSummon: cardPlay.IsAutoPlay));
    }

    internal async Task<Creature?> AutoPlayAndCaptureSummonedCreature(
        PlayerChoiceContext choiceContext,
        Creature? target,
        AutoPlayType type = AutoPlayType.Default,
        bool skipXCapture = false,
        bool skipCardPileVisuals = false
    ) {
        if (_summonResultObserver != null) {
            throw new InvalidOperationException(
                $"A summon result capture is already active for {GetType().Name}."
            );
        }

        Creature? summonedCreature = null;
        _summonResultObserver = creature => summonedCreature ??= creature;
        try {
            await CardCmd.AutoPlay(
                choiceContext,
                this,
                target,
                type,
                skipXCapture,
                skipCardPileVisuals
            );
            return summonedCreature;
        }
        finally {
            _summonResultObserver = null;
        }
    }

    protected virtual async Task<Creature?> SummonMonster(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay
    ) {
        return await SummonMonster(choiceContext, cardPlay, new SummonContext());
    }
    protected virtual async Task<Creature?> SummonMonster(
        PlayerChoiceContext choiceContext,
        CardPlay cardPlay,
        SummonContext summonContext
    ) {
        var c = this.YgoGetMonster();
        if (c == null) return null;
        var combatState = Owner.Creature.CombatState;
        await MonsterSummonHook.BeforeMonsterSummon(
            combatState,
            choiceContext,
            this,
            cardPlay,
            summonContext);
        // Entry.Logger.Info("findMonster");
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
        if (IsUpgraded && summonedCreature.Monster is BaseMonster m) {
            m.SetUpgraded();
        }
        await MonsterSummonHook.AfterMonsterSummon(
            combatState,
            choiceContext,
            this,
            cardPlay,
            summonedCreature,
            summonContext);
        await MonsterCardVfx.PlaySummonCardFly(this, summonedCreature);
        _summonResultObserver?.Invoke(summonedCreature);
        return summonedCreature;
    }

    // //0.107版本
    // protected override PileType GetResultPileTypeForCardPlay() {
    //     return PileType.None;
    // }

    // //0.108版本
    // protected override (PileType, CardPilePosition) GetResultPileTypeAndPositionForCardPlay() {
    //     return (Entry.MonsterPile, CardPilePosition.Bottom);
    // }
    
    //0.109版本
    protected override CardLocation GetResultLocationForCardPlay() {
        return new CardLocation(Owner, Entry.MonsterPile, CardPilePosition.Bottom);
    }

    public int Life => DynamicVars["Life"].IntValue;
    public int Attack => DynamicVars["Attack"].IntValue;
}
