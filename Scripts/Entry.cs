using System.Reflection;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using STS2RitsuLib;
using STS2RitsuLib.CardPiles;
using STS2RitsuLib.Interop;
using System.Text.Json;
using STS2RitsuLib.Audio;
using VYgo.Core;
using VYgo.Core.Cards;
using VYgo.Scripts.Cards;
using VYgo.Scripts.Monsters;
using VYgo.Scripts.Monsters.YGO;
using FileAccess = Godot.FileAccess;
using Logger = MegaCrit.Sts2.Core.Logging.Logger;

namespace VYgo.Scripts;

// 必须要加的属性，用于注册Mod。字符串和初始化函数命名一致。
[ModInitializer(nameof(Initialize))]
public static class Entry {
    public const string ModId = "VYgo";
    public const string ResPath = $"res://{ModId}";

    public static Logger Logger { get; private set; } = null!;
    public static PileType ExtraPile;

    public static Dictionary<int, BaseVYgoCard> CardYgoIdCache { get; private set; } = new();
    public static Dictionary<int, BaseMonster> MonsterYgoIdCache { get; private set; } = new();
    public static Dictionary<int, CoreCard> CoreCardCache { get; private set; } = new();
    
    public static readonly System.Collections.Concurrent.ConcurrentDictionary<string, PackedScene> ModSceneCache = new();
    
    static void LoadScenes() {
        try {
            var paths = CollectAssetPathsSafely();
            if (paths.Count > 0) {
                Logger.Info($"Preloading {paths.Count} RegentFX assets synchronously");
                int success = 0, fail = 0;
                foreach (var path in paths) {
                    try {
                        if (ModSceneCache.ContainsKey(path)) continue;
                        var scene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse);
                        if (scene != null) {
                            ModSceneCache[path] = scene;
                            success++;
                        } else {
                            fail++;
                            Logger.Warn($"Failed to preload: {path}");
                        }
                    } catch (Exception ex) {
                        fail++;
                        Logger.Warn($"Error preloading {path}: {ex.Message}");
                    }
                }
                Logger.Info($"Preloading complete: {success} succeeded, {fail} failed");
            }
        }
        catch (Exception ex) {
            Logger.Warn($"Failed to preload RegentFX assets: {ex.Message}");
        }
    }

    private static List<string> CollectAssetPathsSafely() {
        return [];
    }

    public static void LoadCoreCards() {
        const string path = $"{ResPath}/db.json";
        try {
            var json = FileAccess.GetFileAsString(path);
            if (string.IsNullOrWhiteSpace(json)) {
                Logger.Warn($"Core card database is empty or missing: {path}");
                return;
            }

            var cards = JsonSerializer.Deserialize<List<CoreCard>>(json);

            if (cards == null) {
                Logger.Warn($"Failed to deserialize core card database: {path}");
                return;
            }

            CoreCardCache = new Dictionary<int, CoreCard>();
            foreach (var card in cards) {
                CoreCardCache[card.CardId] = card;
            }

            Logger.Info($"Loaded {CoreCardCache.Count} core cards from {path}");
        } catch (Exception ex) {
            Logger.Warn($"Failed to load core card database from {path}: {ex.Message}");
        }
    }

    public static void Initialize() {
        var assembly = Assembly.GetExecutingAssembly();
        Logger = RitsuLibFramework.CreateLogger(ModId);
        var harmony = new Harmony("sts2.vitech." + ModId.ToLowerInvariant());
        harmony.PatchAll();
        
        RegisterCardPile();
        SubscribeEvents();
        LoadCoreCards();
        
        FmodStudioDeferredBankRegistration.RegisterBank("res://VYgo/banks/VYgo.bank");
        FmodStudioDeferredBankRegistration.RegisterStudioGuidMappings("res://VYgo/banks/VYgo.guids.txt");
        
        RitsuLibFramework.EnsureGodotScriptsRegistered(assembly, Logger);
        ModTypeDiscoveryHub.RegisterModAssembly(ModId, assembly);
        
        Logger.Info("VYgo initialized.");
    }

    //注册额外卡组
    static void RegisterCardPile() {
        var registry = ModCardPileRegistry.For(ModId);
        ExtraPile = registry.RegisterOwned("extra_pile", new ModCardPileSpec {
            Scope = ModCardPileScope.CombatOnly,
            Style = ModCardPileUiStyle.BottomLeft,
            Anchor = ModCardPileAnchor.Default,
            IconPath =  "res://VYgo/images/extra_card_pile.png",
            OnOpen = ctx => ctx.ShowDefaultPileScreen(),
            VisibleWhen = ctx => ctx.Player != null,
        }).PileType;
    }

    static void SubscribeEvents() {
        
        RitsuLibFramework.SubscribeLifecycleOnce<ModelIdsInitializedEvent>(_ =>
        {
            BuildYgoIdCaches();
        });
        // RitsuLibFramework.SubscribeLifecycle<CombatStartingEvent>((@event, disposable) => {
        //     Logger.Info("CombatStarting");
        //     var combatState = @event.CombatState;
        //     foreach (var p in combatState.Players) {
        //         if (p.Character is RedhatCharacter) {
        //             Logger.Info("Found Redhat Character");
        //             //查找额外卡牌
        //             if (p.PlayerCombatState == null) continue;
        //             foreach (var card in p.PlayerCombatState.AllCards) {
        //                 Logger.Info("Found Redhat Character Card:" + card.Title);
        //                 if (card is BaseMonsterCard monsterCard && monsterCard.IsExtra) {
        //                     CardPileCmd.Add(card, ExtraPile);
        //                 }
        //             }
        //         }
        //     }
        // });
    }

    static void BuildYgoIdCaches() {
        CardYgoIdCache = BuildYgoIdCache<BaseVYgoCard>(static () => {
            try {
                return ModelDb.AllCards.OfType<BaseVYgoCard>();
            } catch {
                return Enumerable.Empty<BaseVYgoCard>();
            }
        });

        MonsterYgoIdCache = BuildYgoIdCache<BaseMonster>(static () => [
            ModelDb.Get<CyberDragonMinion>(),
            ModelDb.Get<ProtoCyberDragonMinion>(),
            ModelDb.Get<CyberDragonCoreMinion>(),
            ModelDb.Get<CyberDragonSiegerMinion>(),
            ModelDb.Get<SPLittleKnightMinion>(),
        ]);

        Logger.Info($"Built YGO ID caches: {CardYgoIdCache.Count} cards, {MonsterYgoIdCache.Count} monsters.");
    }

    static Dictionary<int, T> BuildYgoIdCache<T>(Func<IEnumerable<T>> tryGetFromModelDb = null) where T : class, IYgoId {
        var cache = new Dictionary<int, T>();
        try {
            foreach (var item in tryGetFromModelDb()) {
                cache[item.CardId] = item;
            }
        } catch (Exception ex) {
            Logger.Warn($"Failed to build {typeof(T).Name} cache from ModelDb: {ex.Message}");
        }
        return cache;
    }
}