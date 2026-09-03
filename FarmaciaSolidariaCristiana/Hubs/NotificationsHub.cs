using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Hubs;

/// <summary>
/// Hub de SignalR para entrega de notificaciones en tiempo real sobre 443.
/// Es el "push real" para Cuba (donde OneSignal bloquea por IP y FCM no entrega).
/// Si el host (Somee) no soporta WebSocket, el cliente cae automáticamente a SSE/long-polling sobre 443.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationsHub : Hub
{
    private readonly ILogger<NotificationsHub> _logger;
    private readonly IPendingNotificationService _pendingNotificationService;

    public NotificationsHub(
        ILogger<NotificationsHub> logger,
        IPendingNotificationService pendingNotificationService)
    {
        _logger = logger;
        _pendingNotificationService = pendingNotificationService;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Conexión SignalR sin usuario identificable: {ConnectionId}", Context.ConnectionId);
            Context.Abort();
            return;
        }

        // Una conexión por usuario -> grupo "user:{userId}".
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupNameForUser(userId));
        _logger.LogInformation("SignalR conectado: usuario {UserId} conexión {ConnectionId}", userId, Context.ConnectionId);

        // Catch-up: enviar al cliente las notificaciones pendientes no leídas para que
        // no se pierdan las acumuladas mientras estuvo desconectado (cuando SignalR está activo,
        // el polling baja a solo-heartbeat y no las recogería).
        try
        {
            var unread = await _pendingNotificationService.GetUnreadNotificationsAsync(userId);
            foreach (var n in unread)
            {
                await Clients.Caller.SendAsync("ReceiveNotification", new NotificationPayload
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    ReferenceId = n.ReferenceId,
                    ReferenceType = n.ReferenceType,
                    CreatedAt = n.CreatedAt
                });
            }

            if (unread.Count > 0)
            {
                _logger.LogInformation("Catch-up: {Count} notificaciones pendientes enviadas a {UserId} al conectar", unread.Count, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en catch-up de notificaciones para {UserId}", userId);
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupNameForUser(userId));
            _logger.LogInformation("SignalR desconectado: usuario {UserId} conexión {ConnectionId}", userId, Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Nombre de grupo determinístico para un usuario.
    /// </summary>
    public static string GroupNameForUser(string userId) => $"user:{userId}";
}
