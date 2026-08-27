using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Localization.DynamicVars;
using VYgo.Core.Effects;
using VYgo.Core.Hooks;
using MinionLib.Minion;
using STS2RitsuLib.Ui.Toast;
using VYgo.Core;
using VYgo.Core.Summon;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Var;
using VYgo.Utils;

namespace VYgo.Scripts.Cards;

public abstract class BaseMonsterCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : BaseVYgoCard(baseCost, type, rarity, target, showInCardLibrary) {
    private Action<Creature>? _summonResultObserver;
    private SummonContext? _lastSummonContext;
    private bool _isEphemeralMonsterSource;
    private bool _capacityWarningShownForCurrentPlaySeries;

    /// <summary>最近一次召唤的上下文，供配对随从在 OnSummonYgo 中判断是否为特召。</summary>
    internal SummonContext? LastSummonContext => _lastSummonContext;

    /// <summary>
    /// 是否为重放额外结算生成的临时来源卡。临时来源卡只负责给场上怪兽提供唯一同步身份，
    /// 离场相关效果结算后必须从本场战斗移除，不能进入后续抽牌循环。
    /// </summary>
    internal bool IsEphemeralMonsterSource => _isEphemeralMonsterSource;

    protected IHoverTip BaseSummonHoverTip => YgoHoverTipConst.Summon(this);
    protected override IEnumerable<IHoverTip> AdditionalHoverTips => [BaseSummonHoverTip];

    //最大随从数量限制
    protected override bool IsPlayable => Owner.MinionCount() < Owner.GetMaxMinionCount();

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
        if (cardPlay.IsFirstInSeries) {
            _capacityWarningShownForCurrentPlaySeries = false;
        }

        var c = this.YgoGetMonster();
        if (c == null) return null;
        var combatState = Owner.Creature.CombatState;
        _lastSummonContext = summonContext;
        if (!CanSummonAfterCapacityRecheck(cardPlay)) return null;

        await MonsterSummonHook.BeforeMonsterSummon(
            combatState,
            choiceContext,
            this,
            cardPlay,
            summonContext);
        // 召唤前置 Hook 可能改变随从数量或上限，因此在真正创建怪兽前再次确认。
        if (!CanSummonAfterCapacityRecheck(cardPlay)) return null;

        BaseMonsterCard monsterSource = this;
        if (!cardPlay.IsFirstInSeries) {
            monsterSource = (BaseMonsterCard)CreateClone();
            monsterSource.MarkAsEphemeralMonsterSource(summonContext);
            CardPileAddResult addResult = await CardPileCmd.AddGeneratedCardToCombat(
                monsterSource,
                Entry.MonsterPile,
                Owner
            );
            if (!addResult.success) {
                Entry.Logger.Error(
                    $"Failed to add replay monster source {GetType().Name} to the monster pile."
                );
                return null;
            }
        }

        // Entry.Logger.Info("findMonster");
        Creature summonedCreature;
        try {
            summonedCreature = await MinionUtil.AddMinionInstant(
                c.GetType(),
                choiceContext,
                Owner,
                new MinionSummonOptions(
                    MaxHp: Life,
                    PrimaryStatAmount: Attack,
                    Source: monsterSource,
                    Position: MinionPosition.Front
                )
            );
        }
        catch {
            bool sourceIsInUse = Owner.Creature.Pets.Any(pet =>
                pet.Monster is BaseMonster { SourceCard: { } sourceCard }
                && sourceCard == monsterSource);
            if (!sourceIsInUse
                && monsterSource.IsEphemeralMonsterSource
                && monsterSource.Pile?.IsCombatPile == true) {
                await CardPileCmd.RemoveFromCombat(monsterSource, skipVisuals: true);
            }
            throw;
        }
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

    private bool CanSummonAfterCapacityRecheck(CardPlay cardPlay) {
        if (Owner.MinionCount() < Owner.GetMaxMinionCount()) return true;

        if (!_capacityWarningShownForCurrentPlaySeries && LocalContext.IsMe(Owner)) {
            _capacityWarningShownForCurrentPlaySeries = true;
            LocString body = new(
                "combat_messages",
                "V_YGO_SUMMON_ERROR_MINION_CAPACITY.body"
            );
            body.Add("card", Title);
            RitsuToastService.ShowWarning(
                body.GetFormattedText(),
                new LocString(
                    "combat_messages",
                    "V_YGO_SUMMON_ERROR.title"
                ).GetFormattedText()
            );
        }

        Entry.Logger.Info(
            $"Summon of {GetType().Name} at play index {cardPlay.PlayIndex} was blocked " +
            $"by minion capacity ({Owner.MinionCount()}/{Owner.GetMaxMinionCount()})."
        );
        return false;
    }

    private void MarkAsEphemeralMonsterSource(SummonContext summonContext) {
        AssertMutable();
        _isEphemeralMonsterSource = true;
        _lastSummonContext = summonContext;
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
