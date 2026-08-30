using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Combat;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using VYgo.Core;
using VYgo.RitsuAdapters;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;

namespace VYgo.Scripts.Actions;

public abstract class BasePerTurnMonsterAction : ModActionTemplate {
    protected override bool IsVisibleInternal => false;
    public override bool AutoRemoveAtTurnEnd => false;
    public override PowerType Type => PowerType.Buff;
    public override PowerStackType StackType => PowerStackType.None;

    public override string? CustomBigIconPath => CustomIconPath;

    protected abstract string? IntentIconPath { get; }

    protected virtual int? IntentDamage => null;

    protected virtual bool IntentIsAreaAttack => false;

    //每回合使用次数
    protected virtual int MaxUses => 1;
    //本回合剩余使用次数
    public int RemainingUses { get; private set; } = -1;

    private bool _isSelectingTarget;
    private bool _subscribedToOwnerEvents;
    private bool _targetCancelQueued;

    protected void SpendUses() {
        (Owner.GetCreatureNode()?.Visuals as NMonsterVisuals)?
            .PlayActionIntentConfirmFeedback();
        RemainingUses--;
        RefreshActionIntent();
    }
    protected void RecoverUses() {
        RemainingUses = MaxUses;
        RefreshActionIntent();
    }

    public override bool CanAct(ICombatState combatState) {
        Creature owner = Owner;
        return RemainingUses > 0
            && Amount > 0M
            && owner.IsAlive
            && owner.CombatState == combatState
            && combatState.CurrentSide == owner.Side
            && CombatManager.Instance.IsInProgress
            && !CombatManager.Instance.PlayerActionsDisabled
            && RunManager.Instance.ActionQueueSynchronizer.CombatState
                == ActionSynchronizerCombatState.PlayPhase
            && !owner.HasPower<MonsterActionLockedThisTurnPower>();
    }

    public override Task AfterSideTurnStart(CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        foreach (var creature in participants) {
            if (creature.IsPet && creature == Owner) {
                RecoverUses();
                break;
            }
        }
        return Task.CompletedTask;
    }

    public override Task AfterApplied(Creature? applier, CardModel? cardSource) {
        SubscribeToIntentRefreshEvents();
        RecoverUses();
        return Task.CompletedTask;
    }

    public override Task AfterPowerAmountChanged(
        PlayerChoiceContext choiceContext,
        PowerModel power,
        decimal amount,
        Creature? applier,
        CardModel? cardSource
    ) {
        if (power.Owner == Owner)
            RefreshActionIntent();
        return Task.CompletedTask;
    }

    public override Task AfterRemoved(Creature oldOwner) {
        UnsubscribeFromIntentRefreshEvents(oldOwner);
        RefreshActionIntent(oldOwner);
        return Task.CompletedTask;
    }

    public override Task AfterCreatureAddedToCombat(Creature creature) {
        RefreshActionIntent();
        return Task.CompletedTask;
    }

    public override Task AfterCurrentHpChanged(Creature creature, decimal delta) {
        RefreshActionIntent();
        return Task.CompletedTask;
    }

    public override Task AfterDeath(
        PlayerChoiceContext choiceContext,
        Creature creature,
        bool wasRemovalPrevented,
        float deathAnimLength
    ) {
        RefreshActionIntent();
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room) {
        SetIntentState(MonsterActionIntentState.Hidden);
        return Task.CompletedTask;
    }

    internal MonsterActionIntentState CreateIntentState(ICombatState combatState) {
        string? iconPath = IntentIconPath;
        bool hasValidTarget = TargetType == TargetType.None
            || GetValidTargets(combatState).Count > 0;
        bool iconExists = !string.IsNullOrWhiteSpace(iconPath)
            && ResourceLoader.Exists(iconPath);
        bool visible = iconExists && hasValidTarget && CanAct(combatState);

        return new MonsterActionIntentState(
            visible,
            iconPath,
            visible ? IntentDamage : null,
            RemainingUses,
            MaxUses,
            _isSelectingTarget,
            IntentIsAreaAttack
        );
    }

    internal static void RefreshActionIntent(Creature owner) {
        List<BasePerTurnMonsterAction> actions = owner.Powers
            .OfType<BasePerTurnMonsterAction>()
            .ToList();
        if (actions.Count > 1) {
            string monsterType = owner.Monster?.GetType().FullName ?? owner.GetType().FullName ?? "<unknown>";
            string actionTypes = string.Join(", ", actions.Select(action => action.GetType().FullName));
            CardModel? source = (owner.Monster as BaseMonster)?.SourceCard;
            string sourceCard = source == null
                ? "<none>"
                : $"{source.GetType().FullName}({source.Id.Entry})";
            throw new InvalidOperationException(
                $"怪兽可点击行动数量非法：怪兽={monsterType}，现有行动=[{actionTypes}]，源卡={sourceCard}。每只怪兽最多只能安装一个 BasePerTurnMonsterAction。"
            );
        }

        BasePerTurnMonsterAction? action = actions.Count == 1 ? actions[0] : null;
        ICombatState? combatState = owner.CombatState;
        MonsterActionIntentState state = action != null && combatState != null && owner.IsAlive
            ? action.CreateIntentState(combatState)
            : MonsterActionIntentState.Hidden;

        if (action is { _isSelectingTarget: true } && !state.Visible
            && NTargetManager.Instance.IsInSelection) {
            action.QueueTargetingCancelIfStillInvalid();
        }

        (owner.GetCreatureNode()?.Visuals as NMonsterVisuals)?.SetActionIntentState(state);
    }

    protected void RefreshActionIntent() {
        RefreshActionIntent(Owner);
    }

    private void SetIntentState(MonsterActionIntentState state) {
        (Owner.GetCreatureNode()?.Visuals as NMonsterVisuals)?.SetActionIntentState(state);
    }

    private void QueueTargetingCancelIfStillInvalid() {
        if (_targetCancelQueued) return;
        _targetCancelQueued = true;
        NTargetManager targetManager = NTargetManager.Instance;

        Callable.From(() => {
            _targetCancelQueued = false;
            if (!_isSelectingTarget
                || !GodotObject.IsInstanceValid(targetManager)
                || !targetManager.IsInSelection) return;

            ICombatState? combatState = Owner.CombatState;
            if (combatState != null && CreateIntentState(combatState).Visible) return;

            targetManager.CancelTargeting();
        }).CallDeferred();
    }

    private void SubscribeToIntentRefreshEvents() {
        if (_subscribedToOwnerEvents) return;
        _subscribedToOwnerEvents = true;
        Owner.PowerApplied += OnOwnerPowerAppliedOrRemoved;
        Owner.PowerRemoved += OnOwnerPowerAppliedOrRemoved;
        Owner.PowerIncreased += OnOwnerPowerIncreased;
        Owner.PowerDecreased += OnOwnerPowerDecreased;
        Owner.Died += OnOwnerLifeStateChanged;
        Owner.Revived += OnOwnerLifeStateChanged;
        PulsingStarted += OnTargetSelectionStarted;
        PulsingStopped += OnTargetSelectionStopped;
    }

    private void UnsubscribeFromIntentRefreshEvents(Creature oldOwner) {
        if (!_subscribedToOwnerEvents) return;
        _subscribedToOwnerEvents = false;
        oldOwner.PowerApplied -= OnOwnerPowerAppliedOrRemoved;
        oldOwner.PowerRemoved -= OnOwnerPowerAppliedOrRemoved;
        oldOwner.PowerIncreased -= OnOwnerPowerIncreased;
        oldOwner.PowerDecreased -= OnOwnerPowerDecreased;
        oldOwner.Died -= OnOwnerLifeStateChanged;
        oldOwner.Revived -= OnOwnerLifeStateChanged;
        PulsingStarted -= OnTargetSelectionStarted;
        PulsingStopped -= OnTargetSelectionStopped;
    }

    private void OnOwnerPowerAppliedOrRemoved(PowerModel power) {
        RefreshActionIntent();
    }

    private void OnOwnerPowerIncreased(PowerModel power, int change, bool silent) {
        RefreshActionIntent();
    }

    private void OnOwnerPowerDecreased(PowerModel power, bool silent) {
        RefreshActionIntent();
    }

    private void OnOwnerLifeStateChanged(Creature creature) {
        RefreshActionIntent();
    }

    private void OnTargetSelectionStarted() {
        _isSelectingTarget = true;
        RefreshActionIntent();
    }

    private void OnTargetSelectionStopped() {
        _isSelectingTarget = false;
        RefreshActionIntent();
    }
}
