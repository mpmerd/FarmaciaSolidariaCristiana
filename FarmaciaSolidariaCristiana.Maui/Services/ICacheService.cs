namespace FarmaciaSolidariaCristiana.Maui.Services;

public interface ICacheService
{
    bool TryGet<T>(string key, out T? value);
    void Set<T>(string key, T value, TimeSpan? duration = null);
    void Invalidate(string key);
}
