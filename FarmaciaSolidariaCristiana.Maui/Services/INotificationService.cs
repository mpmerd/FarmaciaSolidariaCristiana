namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Interfaz para el servicio de notificaciones push
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Inicializa OneSignal
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// Obtiene el Player ID de OneSignal
    /// </summary>
    string? GetPlayerId();
    
    /// <summary>
    /// Obtiene el Player ID de OneSignal de manera asíncrona con reintentos
    /// </summary>
    /// <param name="maxRetries">Número máximo de reintentos (default: 3)</param>
    /// <param name="delayMs">Delay entre reintentos en ms (default: 500)</param>
    /// <returns>Player ID o null si no está disponible</returns>
    Task<string?> GetPlayerIdAsync(int maxRetries = 3, int delayMs = 500);
    
    /// <summary>
    /// Indica si las notificaciones push están habilitadas
    /// </summary>
    bool IsPushEnabled();
    
    /// <summary>
    /// Registra el dispositivo en el backend
    /// </summary>
    Task<bool> RegisterDeviceAsync();
    
    /// <summary>
    /// Desregistra el dispositivo (logout)
    /// </summary>
    Task<bool> UnregisterDeviceAsync();
    
    /// <summary>
    /// Solicita permisos de notificación
    /// </summary>
    Task RequestPermissionAsync();
    
    /// <summary>
    /// Establece tags del usuario en OneSignal
    /// </summary>
    Task SetUserTagsAsync(string userId, string role);
    
    /// <summary>
    /// Registra el usuario para notificaciones
    /// </summary>
    Task RegisterUserAsync(string userId, string role);
    
    /// <summary>
    /// Desregistra el usuario de notificaciones
    /// </summary>
    Task UnregisterUserAsync();
}
