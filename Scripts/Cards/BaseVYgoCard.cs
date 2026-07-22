using MegaCrit.Sts2.Core.Entities.Cards;
using STS2RitsuLib.Scaffolding.Content;
using VYgo.Core;
using VYgo.Scripts;

namespace VYgo.Scripts.Cards;

public abstract class BaseVYgoCard(
    int baseCost,
    CardType type,
    CardRarity rarity,
    TargetType target,
    bool showInCardLibrary = true)
    : ModCardTemplate(baseCost, type, rarity, target, showInCardLibrary),
        IYgoId {

    // 字段由 Web 工具从 cards.cdb 自动导出到 CoreCard。
    public IReadOnlyList<ushort> ArchetypesList =>
        Entry.CoreCardCache.GetValueOrDefault(CardId)?.Archetypes ?? [];

    public virtual YgoMaterialNames? MaterialCardName => null; //简化的卡名，用于判断素材，检索等
    
    public bool ContainArchetype(YgoArchetypeCode archetype) => ArchetypesList.Contains(archetype.Value);

    public YgoType YgoCardType => CardYgoType;

    public int? Level => this.YgoGetCore() is { HasLevel: true } coreCard ? coreCard.Level : null;

    private static readonly Dictionary<YgoType, string> PORTRAIT = new() {
        [YgoType.normal] = "01",
        [YgoType.effect] = "02",
        [YgoType.spell] = "03",
        [YgoType.trap] = "04",
        [YgoType.synchro] = "05",
        [YgoType.xyz] = "06",
        [YgoType.ritual] = "07",
        [YgoType.fusion] = "08",
        [YgoType.link] = "09",
        [YgoType.token] = "10",
    };

    protected virtual string YgoFramePath {
        get {
            var pNum = PORTRAIT.GetValueOrDefault(CardYgoType, "01");
            return $"res://VYgo/images/frame/{type}/card_design00{pNum}.png";
        }
    }
    
    public abstract int CardId { get; }
    
    protected virtual int PortraitCardId => CardId;

    protected virtual bool UseAncient => false;
    
    
    public override CardAssetProfile AssetProfile => new(
        PortraitPath: $"res://VYgo/images/cards/{PortraitCardId}.png",
        FramePath: YgoFramePath,
        VisualStyle: UseAncient ? CardVisualStyle.Ancient : CardVisualStyle.Standard
    );
    
    protected virtual YgoType CardYgoType => YgoType.effect;
}
