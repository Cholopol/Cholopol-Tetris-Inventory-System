using System.Diagnostics;
using DotPudica.Core.Logging;

namespace Ctis.Core;

/// <summary>
/// Decorator for <see cref="ISaveLoadService"/> that measures execution time and emits performance telemetry.
/// </summary>
public sealed class ProfiledSaveLoadService : ISaveLoadService
{
    private readonly ISaveLoadService _inner;
    private readonly ILog _logger;

    public ProfiledSaveLoadService(ISaveLoadService inner, ILog logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _inner.Restored += OnInnerRestored;
    }

    public event Action? Restored;

    public int SlotCount => _inner.SlotCount;

    public string Serialize()
    {
        long start = Stopwatch.GetTimestamp();
        var result = _inner.Serialize();
        double elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        _logger.Debug($"[SaveLoad] Serialized payload ({result.Length:N0} chars) in {elapsedMs:F2}ms");
        return result;
    }

    public void Restore(string json)
    {
        long start = Stopwatch.GetTimestamp();
        _inner.Restore(json);
        double elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        _logger.Debug($"[SaveLoad] Restored payload ({json?.Length ?? 0:N0} chars) in {elapsedMs:F2}ms");
    }

    public SaveSlotInfo GetSlot(int index) => _inner.GetSlot(index);

    public void SaveSlot(int index)
    {
        long start = Stopwatch.GetTimestamp();
        _inner.SaveSlot(index);
        double elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var info = _inner.GetSlot(index);
        _logger.Info($"[Save] Slot {index} saved in {elapsedMs:F2}ms (Timestamp: {info.Timestamp})");
    }

    public bool LoadSlot(int index)
    {
        long start = Stopwatch.GetTimestamp();
        bool success = _inner.LoadSlot(index);
        double elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        if (success)
        {
            _logger.Info($"[Load] Slot {index} loaded in {elapsedMs:F2}ms");
        }
        else
        {
            _logger.Warn($"[Load] Slot {index} failed to load ({elapsedMs:F2}ms)");
        }
        return success;
    }

    public void DeleteSlot(int index)
    {
        long start = Stopwatch.GetTimestamp();
        _inner.DeleteSlot(index);
        double elapsedMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        _logger.Info($"[SaveLoad] Slot {index} deleted in {elapsedMs:F2}ms");
    }

    private void OnInnerRestored() => Restored?.Invoke();
}
