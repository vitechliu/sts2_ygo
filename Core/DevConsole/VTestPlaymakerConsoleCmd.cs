using System.Security.Cryptography;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.DevConsole;
using MegaCrit.Sts2.Core.DevConsole.ConsoleCommands;
using MegaCrit.Sts2.Core.Entities.CardRewardAlternatives;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Multiplayer;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Nodes.Debug;
using MegaCrit.Sts2.Core.Nodes.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.TestSupport;
using MegaCrit.Sts2.Core.ValueProps;
using MinionLib.Action.GameActions;
using STS2RitsuLib.Interactions.RightClick;
using VYgo.Core.Settings;
using VYgo.Scripts;
using VYgo.Scripts.Actions;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Cards.Category.Playmaker;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Powers;
using VYgo.Utils;

namespace VYgo.Core.DevConsole;

public sealed partial class VTestConsoleCmd {
    private static PlaymakerTestRunner? _playmakerRunner;

    private CmdResult ProcessPlaymaker(Player? player, string[] args) {
        string key = args.FirstOrDefault()?.ToLowerInvariant() ?? "list";
        if (key == "list") return new CmdResult(true, "用法：vtest playmaker all 或具体类名；每项建立新测试战斗。\n" +
            string.Join("\n", PlaymakerTestRunner.Cases.Select(c => $"{c.Key}：{c.Name}")));
        if (key == "status") return new CmdResult(true, _playmakerRunner?.Status ?? "当前没有卡池测试。");
        if (key == "stop") {
            if (_playmakerRunner != null) _playmakerRunner.StopRequested = true;
            return new CmdResult(true, "已请求在当前动作完成后停止，不会继续建立战斗。");
        }
        if (key == "step") {
            var runner = _playmakerRunner;
            if (runner?.StepAction == null || runner.StepBody == null
                || !ReferenceEquals(RunManager.Instance.ActionExecutor.CurrentlyRunningAction, runner.StepAction))
                return new CmdResult(false, "step 仅供正在运行的测试动作调用。");
            return new CmdResult(runner.StepBody(new GameActionPlayerChoiceContext(runner.StepAction)), true);
        }
        if (_playmakerRunner != null) return new CmdResult(false, "已有卡池测试正在运行。");
        string? error = ValidateBattle(player);
        if (error != null) return new CmdResult(false, error);
        var requested = key.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (key != "all" && requested.Any(k => !PlaymakerTestRunner.Cases.Any(c => c.Key.Equals(k, StringComparison.OrdinalIgnoreCase))))
            return new CmdResult(false, "存在未知测试项，请使用 vtest playmaker list。");
        var selected = PlaymakerTestRunner.Cases.Where(c => key == "all" || requested.Contains(c.Key, StringComparer.OrdinalIgnoreCase)).ToArray();
        if (selected.Length == 0) return new CmdResult(false, "没有此测试项，请使用 vtest playmaker list。");
        if (CardSelectCmd.Selector != null || CardSelectCmd.LocalSelector != null
            || RunManager.Instance.ActionExecutor.IsRunning || !RunManager.Instance.ActionQueueSet.IsEmpty)
            return new CmdResult(false, "请等待动作和自动选卡结束。");
        var created = new PlaymakerTestRunner(player!);
        _playmakerRunner = created;
        return new CmdResult(RunPlaymaker(created, selected), true, "卡池测试开始；将替换当前测试战斗，报告自动写入 user://vygo-tests/。");
    }

    private static async Task RunPlaymaker(PlaymakerTestRunner runner, PlaymakerTestRunner.TestCase[] selected) {
        try { await runner.Run(selected); }
        catch (Exception ex) { Entry.Logger.Error("卡池测试中断：" + ex); Report(false, "卡池测试中断：" + ex.Message); }
        finally { if (ReferenceEquals(_playmakerRunner, runner)) _playmakerRunner = null; }
    }
}

// 仅由显式的单机控制台命令启动。场面准备不作为通过证据；断言使用真实结算后的状态。
internal sealed partial class PlaymakerTestRunner(Player player) : ICardSelector {
    internal sealed record TestCase(string Key, string Name, Type? CardType);
    internal static readonly TestCase[] Cases = [
        new("encodetalker", "编码语者", typeof(EncodeTalker)),
        new("excodetalker", "余码语者", typeof(ExcodeTalker)),
        new("powercodetalker", "力码语者", typeof(PowercodeTalker)),
        new("transcodetalker", "转码语者", typeof(TranscodeTalker)),
        new("shootingcodetalker", "排错代码语者", typeof(ShootingcodeTalker)),
        new("decodetalkerheatsoul", "解码语者·炽热之魂", typeof(DecodeTalkerHeatsoul)),
        new("decodetalkerintegration", "解码语者·集成", typeof(DecodeTalkerIntegration)),
        new("decodetalkerextended", "解码语者·扩展", typeof(DecodeTalkerExtended)),
        new("cybersewhitehat", "电子界白帽客", typeof(CyberseWhiteHat)),
        new("cybersegadget", "电子界工具", typeof(CyberseGadget)),
        new("cybersegadgettoken", "工具衍生物", typeof(CyberseGadgetToken)),
        new("dotscaper", "点阵图跳离士", typeof(Dotscaper)),
        new("bootstaggered", "引导交错鹿", typeof(BootStaggered)),
        new("romcloudia", "ROM云雌羊", typeof(ROMCloudia)),
        new("clockwyvern", "时钟翼龙", typeof(ClockWyvern)),
        new("thresholdborg", "阈电压电子人", typeof(ThresholdBorg)),
        new("codegenerator", "代码生成员", typeof(CodeGenerator)),
        new("degradebuster", "退行手雷破坏者", typeof(DegradeBuster)),
        new("accesscodetalker", "访问码语者", typeof(AccesscodeTalker)),
        new("microcoder", "微码编码员", typeof(MicroCoder)),
        new("cynetcodec", "电脑网编解码", typeof(CynetCodec)),
        new("cynetmining", "电脑网挖矿", typeof(CynetMining)),
        new("cynetcrosswipe", "电脑网交叉清除", typeof(CynetCrosswipe)),
        new("cynetuniverse", "电脑网宇宙", typeof(CynetUniverse)),
        new("cynetstorm", "电脑网风暴", typeof(CynetStorm)),
        new("cynetrecovery", "电脑网恢复", typeof(CynetRecovery)),
        new("cynetregression", "电脑网回归", typeof(CynetRegression)),
        new("cynetconflict", "电脑网冲突", typeof(CynetConflict))
    ];

    internal sealed class TestResult {
        public required string Key { get; init; }
        public required string Name { get; init; }
        public bool Upgraded { get; init; }
        public string Status { get; set; } = "运行中";
        public string? Error { get; set; }
        public List<object> Checks { get; } = [];
        public List<object> States { get; } = [];
        public List<object> Choices { get; } = [];
    }

    private readonly List<TestResult> _results = [];
    private TestResult _current = null!;
    private readonly HashSet<CardModel> _preferred = [];
    private int _lastChoiceOptionCount;
    private readonly string _started = DateTimeOffset.Now.ToString("O");
    private readonly string _reportPath = ProjectSettings.GlobalizePath($"user://vygo-tests/playmaker-{DateTime.Now:yyyyMMdd-HHmmss}.json");
    internal bool StopRequested { get; set; }
    internal string Status { get; private set; } = "正在初始化";
    internal ConsoleCmdGameAction? StepAction { get; private set; }
    internal Func<GameActionPlayerChoiceContext, Task>? StepBody { get; private set; }
    private ICombatState Combat => player.Creature.CombatState!;
    private CardPile Hand => PileType.Hand.GetPile(player);
    private CardPile Draw => PileType.Draw.GetPile(player);
    private CardPile Grave => PileType.Discard.GetPile(player);
    private Creature Enemy => Combat.HittableEnemies.First();
    private int Energy => player.PlayerCombatState.Energy;

    internal async Task Run(TestCase[] selected) {
        await Task.Yield();
        NDevConsole.Instance?.HideConsole();
        try {
            foreach (var test in selected) {
                foreach (bool upgraded in test.CardType == null || test.CardType == typeof(CyberseGadgetToken) ? new[] { false } : new[] { false, true }) {
                    if (StopRequested) break;
                    _current = new TestResult { Key = test.Key, Name = test.Name, Upgraded = upgraded };
                    _results.Add(_current);
                    Status = $"{_results.Count}：{test.Name}{(upgraded ? "+" : "")}；报告 {_reportPath}";
                    Entry.Logger.Info("[playmaker-test] 开始 " + Status);
                    Save();
                    if (test.CardType == null) {
                        _current.Status = "阻塞";
                        _current.Error = "当前仓库没有找到对应卡牌/随从实现；表格自动化备注与效果不符。";
                        Save();
                        continue;
                    }
                    try {
                        await NewBattle();
                        // 使用原版 LocalSelector 分支与 PlayerChoiceContext；不将自动选卡视为 UI 或联机验证。
                        using (CardSelectCmd.UseSelector(this, localOnly: true)) {
                            await ExecuteCase(test.CardType, upgraded);
                        }
                        _current.Status = _current.Checks.Count == 0 ? "阻塞" : "通过";
                    }
                    catch (Exception ex) {
                        _current.Status = "失败";
                        _current.Error = ex.Message;
                        Entry.Logger.Error("[playmaker-test] " + test.Name + "：" + ex);
                        // 动作仍在执行时不得继续重建场面，避免将超时误当成已结束。
                        if (ex is TimeoutException || RunManager.Instance.ActionExecutor.IsRunning
                            || !RunManager.Instance.ActionQueueSet.IsEmpty) StopRequested = true;
                    }
                    finally {
                        Capture("结束");
                        Save();
                        Entry.Logger.Info($"[playmaker-test] {_current.Name} 升级={upgraded} {_current.Status} {_current.Error}");
                    }
                }
                if (StopRequested) break;
            }
        }
        finally {
            Status = $"{(StopRequested ? "已停止" : "已结束")}；通过 {_results.Count(r => r.Status == "通过")}，失败 {_results.Count(r => r.Status == "失败")}，阻塞 {_results.Count(r => r.Status == "阻塞")}。报告：{_reportPath}";
            Save();
            Entry.Logger.Info("[playmaker-test] " + Status);
            NDevConsole.Instance?.GetNode<RichTextLabel>("OutputContainer/OutputBuffer").AppendText(Status + "\n");
        }
    }

    private void Save() {
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_reportPath)!);
        System.IO.File.WriteAllText(_reportPath, JsonSerializer.Serialize(new {
            started = _started, updated = DateTimeOffset.Now.ToString("O"), status = Status,
            assemblySha256 = Convert.ToHexString(SHA256.HashData(System.IO.File.ReadAllBytes(typeof(Entry).Assembly.Location))),
            scope = "2026-09-14 藤木游作卡池 29 行 / 28 卡；单机实机结算。自动选卡不验证 UI 操作；联机与完整画面未覆盖。",
            results = _results
        }, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));
    }

    private async Task WaitUntil(Func<bool> condition, string message, int milliseconds = 60000) {
        long deadline = System.Environment.TickCount64 + milliseconds;
        while (!condition()) {
            if (System.Environment.TickCount64 >= deadline) throw new TimeoutException(message);
            await Task.Delay(50);
        }
    }

    private async Task Enqueue(GameAction action) {
        if (StopRequested) throw new OperationCanceledException("用户请求停止测试。");
        RunManager.Instance.ActionQueueSynchronizer.RequestEnqueue(action);
        await action.CompletionTask.WaitAsync(TimeSpan.FromSeconds(60));
        await WaitUntil(() => !RunManager.Instance.ActionExecutor.IsRunning && RunManager.Instance.ActionQueueSet.IsEmpty, "动作队列未结束。");
        if (action.Exception != null) throw action.Exception;
    }

    private async Task Step(Func<GameActionPlayerChoiceContext, Task> body) {
        StepBody = body;
        StepAction = new ConsoleCmdGameAction(player, "vtest playmaker step", inCombat: true);
        try { await Enqueue(StepAction); }
        finally { StepBody = null; StepAction = null; }
    }

    private async Task NewBattle() {
        _preferred.Clear();
        await WaitForCardVisuals();
        var result = new FightConsoleCmd().Process(player, ["BOWLBUGS_WEAK"]);
        if (!result.success) throw new InvalidOperationException(result.msg);
        if (result.task != null) await result.task.WaitAsync(TimeSpan.FromSeconds(60));
        await WaitUntil(() => CombatManager.Instance.IsInProgress && !CombatManager.Instance.PlayerActionsDisabled
            && player.PlayerCombatState.Phase == PlayerTurnPhase.Play
            && RunManager.Instance.ActionQueueSynchronizer.CombatState == ActionSynchronizerCombatState.PlayPhase
            && CombatManager.Instance.IsPartOfPlayerTurn(player) && !RunManager.Instance.ActionExecutor.IsRunning
            && RunManager.Instance.ActionQueueSet.IsEmpty, "新战斗未进入玩家行动阶段。");
        await Step(async choice => {
            var all = player.PlayerCombatState.AllCards.Concat(Entry.ExtraPile.GetPile(player).Cards).Distinct().ToArray();
            await CardPileCmd.RemoveFromCombat(all);
            await WaitForCardVisuals();
            foreach (var creature in Combat.Creatures.ToArray()) {
                foreach (var power in creature.Powers.ToArray()) await PowerCmd.Remove(power);
                await CreatureCmd.LoseBlock(choice, creature, creature.Block, null);
                // 仅调整测试局生命，防止攻击测试意外结束战斗；不改角色最大生命。
                if (creature != player.Creature) await CreatureCmd.SetMaxAndCurrentHp(creature, 1000);
                else await CreatureCmd.SetCurrentHp(creature, creature.MaxHp);
            }
            await PlayerCmd.SetEnergy(30, player);
            for (int i = 0; i < 20; i++) await Add<Bitron>(PileType.Draw);
        });
        Check("新战斗随从区为空", player.MinionCount(), 0);
        Capture("准备完成");
    }

    private async Task WaitForCardVisuals() {
        // 消耗动画会延迟回收 NCard。切场前等待其离树，避免旧回调回收新战斗复用的节点。
        static bool HasExhaustAnimation(Node node) =>
            node is MegaCrit.Sts2.Core.Nodes.Vfx.Cards.NCardExhaustVfx
            || node.GetChildren().Any(HasExhaustAnimation);
        await WaitUntil(() => NCombatRoom.Instance == null || !HasExhaustAnimation(NCombatRoom.Instance),
            "卡牌消耗动画没有结束，停止切换战斗。", 10000);
        await Task.Delay(100);
    }

    private async Task<T> Add<T>(PileType pile, bool upgraded = false) where T : CardModel {
        var card = Combat.CreateCard<T>(player);
        if (upgraded) card.UpgradeInternal();
        if (!(await CardPileCmd.Add(card, pile)).success) throw new InvalidOperationException("测试卡加入牌堆失败：" + card.Id.Entry);
        return card;
    }

    private async Task<CardModel> Add(Type type, bool upgraded) {
        var canonical = ModelDb.AllCards.Single(c => c.GetType() == type);
        var card = Combat.CreateCard(canonical, player);
        if (upgraded) card.UpgradeInternal();
        var pile = card is BaseExtraCard ? Entry.ExtraPile : PileType.Hand;
        if (!(await CardPileCmd.Add(card, pile)).success) throw new InvalidOperationException("加入卡牌失败。");
        return card;
    }

    private async Task Play(CardModel card, Creature? target = null, int? expectedCost = null) {
        int cost = expectedCost ?? card.EnergyCost.GetAmountToSpend();
        Check("出牌前可用", card.CanPlay(), true);
        int before = Energy;
        Capture("出牌前 " + card.Id.Entry);
        await Enqueue(new PlayCardAction(card, target));
        Check("出牌消耗能量 " + card.Id.Entry, before - Energy, cost);
        Check("出牌后离开手牌", card.Pile != Hand, true);
        Capture("出牌后 " + card.Id.Entry);
    }

    private async Task<Creature> Fixture<T>() where T : BaseMonsterCard {
        Creature? creature = null;
        await Step(async choice => {
            var card = await Add<T>(PileType.Hand);
            await VYgoModSettings.RunWithAnimationForTest(player, EffectMode.none, async () => {
                creature = await card.AutoPlayAndCaptureSummonedCreature(choice, null);
            });
        });
        return creature ?? throw new InvalidOperationException("准备素材失败：" + typeof(T).Name);
    }

    private Creature Summoned(CardModel card) => player.Creature.Pets.Single(p => p.IsAlive && p.Monster is BaseMonster m && ReferenceEquals(m.SourceCard, card));
    private int Attack(Creature creature) => creature.GetPowerAmount<AttackPower>();
    private void Stats(CardModel card, int attack, int life) {
        var pet = Summoned(card);
        Check("攻击力", Attack(pet), attack);
        Check("生命", pet.CurrentHp, life);
        Check("来源卡处于怪兽区", card.Pile?.Type == Entry.MonsterPile, true);
        Check("至多一个行动", pet.Powers.OfType<BasePerTurnMonsterAction>().Count() <= 1, true);
    }

    private async Task Link(BaseExtraLinkCard target, params CardModel[] materials) {
        _preferred.Clear();
        foreach (var material in materials) _preferred.Add(material);
        Check("正式额外召唤可发起", DirectExtraDeckSummonNetAction.CanRequest(target), true);
        Capture("连接召唤前 " + target.Id.Entry);
        Check("正式额外召唤已排队", DirectExtraDeckSummonNetAction.Request(target), true);
        await Task.Delay(100);
        await WaitUntil(() => !RunManager.Instance.ActionExecutor.IsRunning && RunManager.Instance.ActionQueueSet.IsEmpty, "连接召唤未完成。");
        Check("结果怪兽登场", player.Creature.Pets.Any(p => p.IsAlive && p.Monster is BaseMonster m && m.SourceCard == target), true);
        foreach (var material in materials) Check("素材已送墓 " + material.Id.Entry, material.Pile == Grave, true);
        _preferred.Clear();
        Capture("连接召唤后 " + target.Id.Entry);
    }

    private async Task StandardLink(BaseExtraLinkCard target) {
        Check("无素材时不可召唤", DirectExtraDeckSummonNetAction.CanRequest(target), false);
        var high = target.YgoGetCore()!.LinkCount == 4 ? await Fixture<DecodeTalker>() : await Fixture<CodeTalker>();
        var low = await Fixture<BackupSecretary>();
        await Link(target, ((BaseMonster)high.Monster!).SourceCard!, ((BaseMonster)low.Monster!).SourceCard!);
    }

    private async Task Act(Creature creature, Creature? target = null) {
        var action = creature.Powers.OfType<BasePerTurnMonsterAction>().Single();
        Check("怪兽行动可用", action.CanAct(Combat), true);
        await Enqueue(new ExecuteCreatureActionGameAction(action, target ?? Enemy));
    }

    private async Task NextTurn() {
        int turn = player.PlayerCombatState.TurnNumber;
        await Step(async choice => {
            foreach (var enemy in Combat.HittableEnemies.ToArray()) await PowerCmd.Apply<StrengthPower>(choice, enemy, -100, null, null);
        });
        await Enqueue(new EndPlayerTurnAction(player, turn));
        await WaitUntil(() => player.PlayerCombatState.TurnNumber > turn && !CombatManager.Instance.PlayerActionsDisabled
            // 玩家回合的 Start 阶段已经递增回合号，但抽牌和回合开始 Hook 尚未结束。
            && player.PlayerCombatState.Phase == PlayerTurnPhase.Play
            && RunManager.Instance.ActionQueueSynchronizer.CombatState == ActionSynchronizerCombatState.PlayPhase
            && CombatManager.Instance.IsPartOfPlayerTurn(player) && !RunManager.Instance.ActionExecutor.IsRunning
            && RunManager.Instance.ActionQueueSet.IsEmpty, "没有完成真实回合转换。");
        Capture("下回合开始");
    }

    private async Task RightClick(AbstractModel model) {
        await Step(async choice => {
            var context = new ModRightClickExecutionContext(player, model, new ModRightClickTrigger(), choice, StepAction);
            if (model is IModRightClickableCard card) await card.OnRightClick(context);
            else if (model is IModRightClickablePower power) await power.OnRightClick(context);
            else throw new InvalidOperationException("模型没有右键入口。");
        });
    }

    private void Check<T>(string name, T actual, T expected) {
        bool pass = EqualityComparer<T>.Default.Equals(actual, expected);
        _current.Checks.Add(new { name, actual, expected, pass });
        if (!pass) throw new InvalidOperationException($"{name}：预期 {expected}，实际 {actual}");
    }

    private void Capture(string stage) {
        _current.States.Add(new { stage, turn = player.PlayerCombatState?.TurnNumber, energy = player.PlayerCombatState?.Energy,
            playerHp = player.Creature.CurrentHp,
            enemies = player.Creature.CombatState?.HittableEnemies.Select(c => new { c.Name, hp = c.CurrentHp, maxHp = c.MaxHp, c.Block }).ToArray(),
            pets = player.Creature.Pets.Select(c => new { id = (c.Monster as BaseMonster)?.SourceCard?.Id.Entry, hp = c.CurrentHp, attack = c.GetPowerAmount<AttackPower>(), alive = c.IsAlive }).ToArray(),
            hand = Hand.Cards.Select(c => c.Id.Entry).ToArray(), grave = Grave.Cards.Select(c => c.Id.Entry).ToArray(),
            draw = Draw.Cards.Count, exhaust = PileType.Exhaust.GetPile(player).Cards.Select(c => c.Id.Entry).ToArray() });
    }

    public Task<IEnumerable<CardModel>> GetSelectedCards(IEnumerable<CardModel> options, int minSelect, int maxSelect) {
        var list = options.ToArray();
        var preferred = list.Where(_preferred.Contains).ToArray();
        // 发现选卡的选择器下限可为 0；用例需要实际选择，不能把“跳过”误当作获得卡牌。
        var selected = preferred.Length > 0 && preferred.Length >= minSelect && preferred.Length <= maxSelect
            ? preferred : list.Take(Math.Min(maxSelect, Math.Max(1, minSelect))).ToArray();
        if (selected.Length < minSelect) throw new InvalidOperationException("自动选卡候选不足。");
        _lastChoiceOptionCount = list.Length;
        _current.Choices.Add(new { minSelect, maxSelect, options = list.Select(c => new { id = c.Id.Entry, c.IsUpgraded }).ToArray(), selected = selected.Select(c => c.Id.Entry).ToArray() });
        _preferred.Clear();
        return Task.FromResult<IEnumerable<CardModel>>(selected);
    }

    public CardRewardSelection GetSelectedCardReward(IReadOnlyList<CardCreationResult> options, IReadOnlyList<CardRewardAlternative> alternatives) =>
        throw new NotSupportedException("本脚本不选择战斗奖励。");
}
