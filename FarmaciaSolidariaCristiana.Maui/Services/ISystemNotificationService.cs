namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Muestra notificaciones nativas del sistema (barra de estado + sonido).
/// Implementación Android vía NotificationCompat. Para iOS quedaría pendiente (la app es solo Android hoy).
/// </summary>
public interface ISystemNotificationService
{
    /// <summary>
    /// Muestra una notificación del sistema con sonido y acción "Ver".
    /// </summary>
    Task ShowAsync(string title, string message, string notificationType, int? referenceId = null);
}
