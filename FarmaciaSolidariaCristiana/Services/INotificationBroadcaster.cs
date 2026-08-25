namespace FarmaciaSolidariaCristiana.Services;

/// <summary>
/// Difunde notificaciones en tiempo real a un usuario vía SignalR (canal sobre 443).
/// El "push real" para Cuba cuando FCM/OneSignal no entrega.
/// </summary>
public interface INotificationBroadcaster
{
    /// <summary>
    /// Envía una notificación al usuario en tiempo real (si tiene una conexión SignalR activa).
    /// No lanza si el usuario no está conectado (simplemente no llega en tiempo real; queda pendiente para polling).
    /// </summary>
    Task BroadcastToUserAsync(string userId, NotificationPayload payload);
}

/// <summary>
/// Payload de la notificación que viaja por SignalR hacia la app MAUI.
/// Mismo contrato que PendingNotificationDto (incluye Id para de-duplicación).
/// </summary>
public class NotificationPayload
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime CreatedAt { get; set; }
}
