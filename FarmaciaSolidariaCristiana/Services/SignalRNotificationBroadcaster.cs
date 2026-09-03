using FarmaciaSolidariaCristiana.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace FarmaciaSolidariaCristiana.Services;

/// <summary>
/// Implementación de INotificationBroadcaster usando IHubContext&lt;NotificationsHub&gt;.
/// </summary>
public class SignalRNotificationBroadcaster : INotificationBroadcaster
{
    private readonly IHubContext<NotificationsHub> _hubContext;
    private readonly ILogger<SignalRNotificationBroadcaster> _logger;

    public SignalRNotificationBroadcaster(
        IHubContext<NotificationsHub> hubContext,
        ILogger<SignalRNotificationBroadcaster> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task BroadcastToUserAsync(string userId, NotificationPayload payload)
    {
        try
        {
            await _hubContext.Clients
                .Group(NotificationsHub.GroupNameForUser(userId))
                .SendAsync("ReceiveNotification", payload);
        }
        catch (Exception ex)
        {
            // No es fatal: la notificación ya quedó pendiente en BD para polling.
            _logger.LogError(ex, "Error difundiendo notificación por SignalR a usuario {UserId}", userId);
        }
    }
}
