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
