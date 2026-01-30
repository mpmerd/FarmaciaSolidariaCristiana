namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Servicio de polling para obtener notificaciones cuando push no está disponible.
/// Este servicio consulta periódicamente al servidor para verificar si hay nuevas notificaciones.
/// Es la alternativa a push notifications para Cuba donde FCM no funciona.
/// </summary>
public interface IPollingNotificationService
{
    /// <summary>
    /// Evento que se dispara cuando hay nuevas notificaciones
    /// </summary>
    event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;

    /// <summary>
    /// Inicia el servicio de polling (llamar después del login)
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Detiene el servicio de polling (llamar en logout)
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Fuerza una verificación inmediata de notificaciones
    /// </summary>
    Task<int> CheckNowAsync();

    /// <summary>
    /// Obtiene el conteo de notificaciones no leídas
    /// </summary>
    Task<int> GetUnreadCountAsync();

    /// <summary>
    /// Marca una notificación como leída
    /// </summary>
    Task<bool> MarkAsReadAsync(int notificationId);

    /// <summary>
    /// Marca todas las notificaciones como leídas
    /// </summary>
    Task<int> MarkAllAsReadAsync();

    /// <summary>
    /// Indica si el servicio está activo
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// Intervalo de polling en segundos
    /// </summary>
    int PollingIntervalSeconds { get; set; }
}

/// <summary>
/// Argumentos del evento de notificación recibida
/// </summary>
public class NotificationReceivedEventArgs : EventArgs
{
    public int NotificationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO para la respuesta de notificaciones pendientes
/// </summary>
public class PendingNotificationsResponse
{
    public int Count { get; set; }
    public List<PendingNotificationItem> Notifications { get; set; } = new();
}

/// <summary>
/// DTO para una notificación pendiente
/// </summary>
public class PendingNotificationItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime CreatedAt { get; set; }
}
