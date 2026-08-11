using STS2RitsuLib;
using STS2RitsuLib.Data;
using STS2RitsuLib.Utils.Persistence;

namespace VYgo.Core.Saves;

/// <summary>
/// RitsuLib <see cref="SaveScope.Profile"/> 持久化槽的通用基类。
/// </summary>
/// <typeparam name="TData">可由 System.Text.Json 序列化的存档根对象。</typeparam>
public abstract class ProfileSave<TData> : IDisposable where TData : class, new() {
    private readonly ModDataStore _store;
    private readonly string _key;
    private readonly string _fileName;
    private readonly bool _syncToCloud;
    private ModDataStoreCache<TData>? _cache;
    private bool _disposed;

    protected ProfileSave(
        string modId,
        string key,
        string fileName,
        bool syncToCloud = true) {
        ArgumentException.ThrowIfNullOrWhiteSpace(modId);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        _store = RitsuLibFramework.GetDataStore(modId);
        _key = key;
        _fileName = fileName;
        _syncToCloud = syncToCloud;
    }

    /// <summary>
    /// 当前 Profile 数据是否已经可以安全读写。
    /// </summary>
    public bool IsReady => _cache != null && _store.IsProfileInitialized;

    /// <summary>
    /// 注册存储槽。应在 Mod 初始化期间的 BeginModDataRegistration 作用域中调用一次。
    /// </summary>
    public void Register() {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_cache != null) {
            throw new InvalidOperationException($"Profile 存储槽 '{_key}' 已经注册。");
        }

        _store.Register(
            key: _key,
            fileName: _fileName,
            scope: SaveScope.Profile,
            syncToCloud: _syncToCloud,
            defaultFactory: CreateDefault,
            autoCreateIfMissing: true);
        _cache = _store.CreateCache<TData>(_key);
    }

    /// <summary>
    /// 创建新 Profile 使用的默认数据。
    /// </summary>
    protected virtual TData CreateDefault() => new();

    /// <summary>
    /// 读取当前 Profile 数据。不要让读取器返回根对象或其可变子对象。
    /// </summary>
    public TResult Read<TResult>(Func<TData, TResult> reader) {
        ArgumentNullException.ThrowIfNull(reader);
        EnsureReady();
        return reader(_cache!.Value);
    }

    /// <summary>
    /// 尝试读取当前 Profile 数据；档案服务尚未就绪时返回 false。
    /// </summary>
    public bool TryRead<TResult>(Func<TData, TResult> reader, out TResult result) {
        ArgumentNullException.ThrowIfNull(reader);
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureRegistered();

        if (!IsReady) {
            result = default!;
            return false;
        }

        result = reader(_cache!.Value);
        return true;
    }

    /// <summary>
    /// 原地修改当前 Profile 数据，并默认立即写盘。
    /// 高频连续修改时可传 false，最后显式调用 Save 合并写盘。
    /// </summary>
    public void Modify(Action<TData> modifier, bool saveImmediately = true) {
        ArgumentNullException.ThrowIfNull(modifier);
        EnsureReady();

        _cache!.Modify(modifier);
        if (saveImmediately) {
            _cache.Save();
        }
    }

    /// <summary>
    /// 将当前 Profile 数据显式写入磁盘。
    /// </summary>
    public void Save() {
        EnsureReady();
        _cache!.Save();
    }

    public void Dispose() {
        if (_disposed) return;

        _disposed = true;
        _cache?.Dispose();
        _cache = null;
        GC.SuppressFinalize(this);
    }

    private void EnsureRegistered() {
        if (_cache == null) {
            throw new InvalidOperationException($"Profile 存储槽 '{_key}' 尚未注册。");
        }
    }

    private void EnsureReady() {
        ObjectDisposedException.ThrowIf(_disposed, this);
        EnsureRegistered();
        if (!_store.IsProfileInitialized) {
            throw new InvalidOperationException("当前 Profile 持久化数据尚未就绪。");
        }
    }
}
