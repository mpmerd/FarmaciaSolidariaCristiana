using FarmaciaSolidariaCristiana.Maui.Helpers;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// DTO de la notificación que llega por SignalR (mismo contrato que el backend NotificationPayload).
/// </summary>
public class HubNotificationPayload
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Cliente del hub de SignalR (canal sobre 443 = "push real" para Cuba).
/// Mantiene una conexión persistente con reconexión automática y backoff exponencial.
/// Al recibir una notificación: reporta la entrega al PushHealthService (de-dup) y
/// dispara el evento NotificationReceived (para refresco de UI) y la notificación del sistema.
/// </summary>
public interface INotificationsHubClient
{
    /// <summary>Se dispara cuando llega una notificación por SignalR.</summary>
    event EventHandler<NotificationReceivedEventArgs>? NotificationReceived;

    /// <summary>True si la conexión está activa.</summary>
    bool IsConnected { get; }

    /// <summary>Inicia la conexión (requiere JWT válido).</summary>
    Task StartAsync();

    /// <summary>Detiene la conexión.</summary>
    Task StopAsync();
}
