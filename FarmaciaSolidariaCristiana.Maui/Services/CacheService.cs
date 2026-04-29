using System.Collections.Concurrent;

namespace FarmaciaSolidariaCristiana.Maui.Services;

public class CacheService : ICacheService
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, (object Value, DateTime ExpiresAt)> _cache = new();

    public bool TryGet<T>(string key, out T? value)
    {
        if (_cache.TryGetValue(key, out var entry) && DateTime.UtcNow < entry.ExpiresAt)
        {
            value = (T)entry.Value;
            return true;
        }
        value = default;
        return false;
    }

    public void Set<T>(string key, T value, TimeSpan? duration = null)
    {
        var expiresAt = DateTime.UtcNow + (duration ?? DefaultDuration);
        _cache[key] = (value!, expiresAt);
    }

    public void Invalidate(string key)
        => _cache.TryRemove(key, out _);
}
