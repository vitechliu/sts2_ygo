namespace VYgo.Core.Saves;

/// <summary>
/// VYgo 的 Profile 级持久化入口。
/// </summary>
public sealed class YgoSave : ProfileSave<YgoSaveData> {
    public const string DataKey = "profile_progress";
    public const string FileName = "profile_progress.json";

    public static YgoSave Instance { get; } = new();

    private YgoSave() : base(Scripts.Entry.ModId, DataKey, FileName) { }

    /// <summary>
    /// 获取指定角色的进度快照。characterId 必须是稳定 ID，不能使用本地化名称。
    /// </summary>
    public CharacterProgressSnapshot GetCharacterProgress(string characterId) {
        ValidateCharacterId(characterId);
        return Read(data => CreateSnapshot(data, characterId));
    }

    /// <summary>
    /// 档案尚未就绪时返回 false；角色没有记录时返回一级零经验的默认快照。
    /// </summary>
    public bool TryGetCharacterProgress(string characterId, out CharacterProgressSnapshot progress) {
        ValidateCharacterId(characterId);
        return TryRead(data => CreateSnapshot(data, characterId), out progress);
    }

    /// <summary>
    /// 修改指定角色的进度。首次修改该角色时会自动创建默认记录。
    /// </summary>
    public void ModifyCharacterProgress(
        string characterId,
        Action<CharacterProgressData> modifier,
        bool saveImmediately = true) {
        ValidateCharacterId(characterId);
        ArgumentNullException.ThrowIfNull(modifier);

        Modify(data => {
            data.Normalize();
            if (!data.Characters.TryGetValue(characterId, out CharacterProgressData? progress)) {
                progress = new CharacterProgressData();
                data.Characters[characterId] = progress;
            }

            modifier(progress);
            progress.Normalize();
        }, saveImmediately);
    }

    private static CharacterProgressSnapshot CreateSnapshot(YgoSaveData data, string characterId) {
        data.Normalize();
        return data.Characters.TryGetValue(characterId, out CharacterProgressData? progress)
            ? new CharacterProgressSnapshot(
                Math.Max(CharacterProgressData.MinimumLevel, progress.Level),
                Math.Max(0L, progress.Experience))
            : CharacterProgressSnapshot.Default;
    }

    private static void ValidateCharacterId(string characterId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(characterId);
    }
}

/// <summary>
/// VYgo Profile 存档的 JSON 根对象。
/// 新增持久化概念时优先在这里增加带默认值的 public property，并保持旧属性名不变。
/// </summary>
public sealed class YgoSaveData {
    public Dictionary<string, CharacterProgressData> Characters { get; set; } = new();

    // 为简单的解锁、开关和累计值预留强类型容器；复杂数据应新增专用 class/property。
    public HashSet<string> UnlockedContentIds { get; set; } = [];
    public Dictionary<string, bool> Flags { get; set; } = new();
    public Dictionary<string, long> Counters { get; set; } = new();

    internal void Normalize() {
        Characters ??= new();
        UnlockedContentIds ??= [];
        Flags ??= new();
        Counters ??= new();
    }
}

/// <summary>
/// 单个角色的长期进度。后续角色专属内容可继续增加带默认值的属性。
/// </summary>
public sealed class CharacterProgressData {
    public const int MinimumLevel = 1;

    public int Level { get; set; } = MinimumLevel;
    public long Experience { get; set; }
    public HashSet<string> UnlockedContentIds { get; set; } = [];
    public Dictionary<string, long> Counters { get; set; } = new();

    internal void Normalize() {
        Level = Math.Max(MinimumLevel, Level);
        Experience = Math.Max(0L, Experience);
        UnlockedContentIds ??= [];
        Counters ??= new();
    }
}

/// <summary>
/// 提供给 UI 和玩法代码的只读角色进度快照，避免外部绕过 Save 修改活动存档对象。
/// </summary>
public readonly record struct CharacterProgressSnapshot(int Level, long Experience) {
    public static CharacterProgressSnapshot Default { get; } =
        new(CharacterProgressData.MinimumLevel, 0L);
}
