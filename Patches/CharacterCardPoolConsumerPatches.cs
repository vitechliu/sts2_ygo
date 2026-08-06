using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Unlocks;
using VYgo.Core.CardPools;
using VYgo.Scripts;

namespace VYgo.Patches;

/// <summary>
/// Redirects selected vanilla card-pool reads to all pools linked to a YGO character.
///
/// Targets are resolved by optional type/method names so a renamed or removed vanilla method cannot fail mod
/// initialization. This class is applied separately from PatchAll, and every target is patched independently.
/// </summary>
public static class CharacterCardPoolConsumerPatches {
    private sealed record TargetSpec(string TypeName, string MethodName, bool IsAsync = true);

    private static readonly MethodInfo? GetUnlockedCardsMethod = AccessTools.Method(
        typeof(CardPoolModel),
        nameof(CardPoolModel.GetUnlockedCards),
        [typeof(UnlockState), typeof(CardMultiplayerConstraint)]);

    private static readonly MethodInfo? GetLinkedUnlockedCardsMethod = AccessTools.Method(
        typeof(CharacterCardPoolConsumerPatches),
        nameof(GetLinkedUnlockedCards));

    private static readonly MethodInfo? GetAllCardsMethod = AccessTools.PropertyGetter(
        typeof(CardPoolModel),
        nameof(CardPoolModel.AllCards));

    private static readonly MethodInfo? GetLinkedAllCardsMethod = AccessTools.Method(
        typeof(CharacterCardPoolConsumerPatches),
        nameof(GetLinkedAllCards));

    // Keep this list deliberately narrow. Colorless-only generators are not compatibility targets.
    // String lookup makes every entry optional when the game updates.
    private static readonly TargetSpec[] KnownTargets = [
        // Cards which generate cards from Owner.Character.CardPool.
        new("MegaCrit.Sts2.Core.Models.Cards.Abundance", "OnPlay"),
        new("MegaCrit.Sts2.Core.Models.Cards.Discovery", "OnPlay"),
        new("MegaCrit.Sts2.Core.Models.Cards.Distraction", "OnPlay"),
        new("MegaCrit.Sts2.Core.Models.Cards.Fasten", "get_ExtraHoverTips", IsAsync: false),
        new("MegaCrit.Sts2.Core.Models.Cards.InfernalBlade", "OnPlay"),
        new("MegaCrit.Sts2.Core.Models.Cards.Jackpot", "OnPlay"),
        new("MegaCrit.Sts2.Core.Models.Cards.MadScience", "ExecuteRider"),
        new("MegaCrit.Sts2.Core.Models.Cards.Metamorphosis", "OnPlay"),
        new("MegaCrit.Sts2.Core.Models.Cards.Stoke", "OnPlay"),
        new("MegaCrit.Sts2.Core.Models.Cards.WhiteNoise", "OnPlay"),

        // Relics with direct pool reads or minimum-card-count checks.
        new("MegaCrit.Sts2.Core.Models.Relics.BigHat", "AfterSideTurnStart"),
        new("MegaCrit.Sts2.Core.Models.Relics.ChoicesParadox", "AfterPlayerTurnStart"),
        new("MegaCrit.Sts2.Core.Models.Relics.Crossbow", "AfterSideTurnStart"),
        new("MegaCrit.Sts2.Core.Models.Relics.DustyTome", "SetupForPlayer", IsAsync: false),
        new("MegaCrit.Sts2.Core.Models.Relics.LargeCapsule", "GetDefendForCharacter", IsAsync: false),
        new("MegaCrit.Sts2.Core.Models.Relics.LargeCapsule", "GetStrikeForCharacter", IsAsync: false),
        new("MegaCrit.Sts2.Core.Models.Relics.ScrollBoxes", "CanGenerateBundles", IsAsync: false),
        new("MegaCrit.Sts2.Core.Models.Relics.VexingPuzzlebox", "AfterPlayerTurnStart"),

        // Potions which generate a character attack, skill, or power.
        new("MegaCrit.Sts2.Core.Models.Potions.AttackPotion", "OnUse"),
        new("MegaCrit.Sts2.Core.Models.Potions.OrobicAcid", "OnUse"),
        new("MegaCrit.Sts2.Core.Models.Potions.PowerPotion", "OnUse"),
        new("MegaCrit.Sts2.Core.Models.Potions.SkillPotion", "OnUse"),

        // Powers which generate cards from the owning character.
        new("MegaCrit.Sts2.Core.Models.Powers.CalamityPower", "AfterCardPlayed"),
        new("MegaCrit.Sts2.Core.Models.Powers.CallOfTheVoidPower", "BeforeHandDraw"),
        new("MegaCrit.Sts2.Core.Models.Powers.CreativeAiPower", "BeforeHandDraw"),
        new("MegaCrit.Sts2.Core.Models.Powers.HelloWorldPower", "BeforeHandDraw"),

        // Events and other transform callers pass through this factory method.
        new("MegaCrit.Sts2.Core.Factories.CardFactory", "GetDefaultTransformationOptions", IsAsync: false),
    ];

    private static int _runtimeFallbackWarningLogged;

    public static void Apply(Harmony harmony) {
        try {
            var transpilerMethod = AccessTools.Method(
                typeof(CharacterCardPoolConsumerPatches),
                nameof(RedirectPrimaryCardPoolCalls));
            if (transpilerMethod == null) {
                SafeWarn("Card-pool consumer compatibility disabled: transpiler method could not be resolved.");
                return;
            }

            var patchedCount = 0;
            foreach (var target in TargetMethods()) {
                try {
                    harmony.Patch(target, transpiler: new HarmonyMethod(transpilerMethod));
                    patchedCount++;
                }
                catch (Exception exception) {
                    SafeWarn(
                        $"Card-pool consumer compatibility could not patch {DescribeMethod(target)}; " +
                        $"that method keeps its original behavior. {exception.GetType().Name}: {exception.Message}");
                }
            }

            SafeInfo($"Card-pool consumer compatibility applied to {patchedCount} vanilla call sites.");
        }
        catch (Exception exception) {
            SafeWarn(
                "Card-pool consumer compatibility setup failed; the rest of VYgo will continue loading. " +
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    public static IEnumerable<MethodBase> TargetMethods() {
        if (GetUnlockedCardsMethod == null ||
            GetLinkedUnlockedCardsMethod == null ||
            GetAllCardsMethod == null ||
            GetLinkedAllCardsMethod == null) {
            SafeWarn("Card-pool consumer compatibility disabled: required method could not be resolved.");
            return [];
        }

        try {
            var targets = new HashSet<MethodBase>();
            var skipped = new List<string>();
            var vanillaAssembly = typeof(CardModel).Assembly;

            foreach (var spec in KnownTargets) {
                ResolveTargets(vanillaAssembly, spec, targets, skipped);
            }

            if (skipped.Count > 0) {
                SafeWarn(
                    $"Card-pool consumer compatibility skipped {skipped.Count} changed or missing target(s): " +
                    string.Join(", ", skipped) + ". Original behavior is preserved for them.");
            }

            if (targets.Count == 0) {
                SafeWarn(
                    "Card-pool consumer compatibility found no matching vanilla call sites. " +
                    "The game may have changed; original behavior will be preserved.");
            }
            else {
                SafeInfo($"Card-pool consumer compatibility will patch {targets.Count} vanilla call sites.");
            }

            return targets;
        }
        catch (Exception exception) {
            SafeWarn(
                "Card-pool consumer compatibility target resolution failed; original behavior will be preserved. " +
                $"{exception.GetType().Name}: {exception.Message}");
            return [];
        }
    }

    public static IEnumerable<CodeInstruction> RedirectPrimaryCardPoolCalls(
        IEnumerable<CodeInstruction> instructions,
        MethodBase __originalMethod) {
        var code = instructions.ToList();

        try {
            if (GetUnlockedCardsMethod == null ||
                GetLinkedUnlockedCardsMethod == null ||
                GetAllCardsMethod == null ||
                GetLinkedAllCardsMethod == null) {
                return code;
            }

            var matchCount = 0;
            foreach (var instruction in code) {
                if (instruction.operand is not MethodInfo called) continue;

                if (called == GetUnlockedCardsMethod) {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = GetLinkedUnlockedCardsMethod;
                    matchCount++;
                }
                else if (called == GetAllCardsMethod) {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = GetLinkedAllCardsMethod;
                    matchCount++;
                }
            }

            if (matchCount == 0) {
                SafeWarn(
                    $"Card-pool consumer compatibility no longer matches {DescribeMethod(__originalMethod)}; " +
                    "that method was left unchanged.");
                return code;
            }

            return code;
        }
        catch (Exception exception) {
            SafeWarn(
                $"Card-pool consumer compatibility failed to transpile {DescribeMethod(__originalMethod)}; " +
                $"that method was left unchanged. {exception.GetType().Name}: {exception.Message}");
            return code;
        }
    }

    public static IEnumerable<CardModel> GetLinkedUnlockedCards(
        CardPoolModel pool,
        UnlockState unlockState,
        CardMultiplayerConstraint multiplayerConstraint) {
        try {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null) return pool.GetUnlockedCards(unlockState, multiplayerConstraint);

            Player? player = runState.Players.FirstOrDefault(candidate =>
                ReferenceEquals(candidate.UnlockState, unlockState) &&
                candidate.IsYgoCharacter() &&
                CharacterCardPoolLinks.HasExtraPools(candidate.Character) &&
                CharacterCardPoolLinks.GetPoolsFor(candidate.Character).Any(linkedPool => linkedPool.Id == pool.Id));

            if (player == null) return pool.GetUnlockedCards(unlockState, multiplayerConstraint);

            return CharacterCardPoolLinks
                .GetUnlockedCardsFor(player, multiplayerConstraint)
                .ToArray();
        }
        catch (Exception exception) {
            LogRuntimeFallbackOnce(exception);
            return pool.GetUnlockedCards(unlockState, multiplayerConstraint);
        }
    }

    public static IEnumerable<CardModel> GetLinkedAllCards(CardPoolModel pool) {
        try {
            var runState = RunManager.Instance.DebugOnlyGetState();
            if (runState == null) return pool.AllCards;

            Player? player = runState.Players.FirstOrDefault(candidate =>
                candidate.IsYgoCharacter() &&
                CharacterCardPoolLinks.HasExtraPools(candidate.Character) &&
                CharacterCardPoolLinks.GetPoolsFor(candidate.Character).Any(linkedPool => linkedPool.Id == pool.Id));

            if (player == null) return pool.AllCards;

            return CharacterCardPoolLinks
                .GetPoolsFor(player.Character)
                .SelectMany(static linkedPool => linkedPool.AllCards)
                .DistinctBy(static card => card.Id)
                .ToArray();
        }
        catch (Exception exception) {
            LogRuntimeFallbackOnce(exception);
            return pool.AllCards;
        }
    }

    private static void ResolveTargets(
        Assembly vanillaAssembly,
        TargetSpec spec,
        ISet<MethodBase> targets,
        ICollection<string> skipped) {
        try {
            var type = vanillaAssembly.GetType(spec.TypeName, throwOnError: false);
            if (type == null) {
                skipped.Add($"{spec.TypeName}.{spec.MethodName} (type missing)");
                return;
            }

            const BindingFlags flags = BindingFlags.Public |
                                       BindingFlags.NonPublic |
                                       BindingFlags.Instance |
                                       BindingFlags.Static |
                                       BindingFlags.DeclaredOnly;
            var declaredMethods = type
                .GetMethods(flags)
                .Where(method => method.Name == spec.MethodName)
                .ToList();

            if (declaredMethods.Count == 0) {
                skipped.Add($"{spec.TypeName}.{spec.MethodName} (method missing)");
                return;
            }

            var matches = new List<MethodBase>();
            foreach (var declaredMethod in declaredMethods) {
                MethodBase? patchTarget = declaredMethod;
                if (spec.IsAsync) {
                    patchTarget = AccessTools.AsyncMoveNext(declaredMethod);
                }

                if (patchTarget != null && CallsSupportedPoolRead(patchTarget)) {
                    matches.Add(patchTarget);
                }
            }

            if (matches.Count == 0) {
                skipped.Add($"{spec.TypeName}.{spec.MethodName} (expected call missing)");
                return;
            }

            foreach (var match in matches) targets.Add(match);
        }
        catch (Exception exception) {
            skipped.Add($"{spec.TypeName}.{spec.MethodName} ({exception.GetType().Name})");
        }
    }

    private static bool CallsSupportedPoolRead(MethodBase method) {
        try {
            return PatchProcessor
                .GetOriginalInstructions(method)
                .Any(instruction =>
                    instruction.operand is MethodInfo called &&
                    (called == GetUnlockedCardsMethod || called == GetAllCardsMethod));
        }
        catch {
            return false;
        }
    }

    private static void LogRuntimeFallbackOnce(Exception exception) {
        if (Interlocked.Exchange(ref _runtimeFallbackWarningLogged, 1) != 0) return;

        SafeWarn(
            "Card-pool consumer compatibility failed at runtime and will use the original pool. " +
            $"Further warnings are suppressed. {exception.GetType().Name}: {exception.Message}");
    }

    private static string DescribeMethod(MethodBase method) {
        return $"{method.DeclaringType?.FullName ?? "<unknown>"}.{method.Name}";
    }

    private static void SafeInfo(string message) {
        try {
            Entry.Logger?.Info(message);
        }
        catch {
            // Logging must never make this optional compatibility patch fatal.
        }
    }

    private static void SafeWarn(string message) {
        try {
            Entry.Logger?.Warn(message);
        }
        catch {
            // Logging must never make this optional compatibility patch fatal.
        }
    }
}
