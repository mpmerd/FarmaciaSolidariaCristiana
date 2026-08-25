using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FarmaciaSolidariaCristiana.Hubs;

/// <summary>
/// Hub de SignalR para entrega de notificaciones en tiempo real sobre 443.
/// Es el "push real" para usuarios en Cuba (donde FCM/OneSignal no entrega por bloqueo del puerto 5228).
/// Si el host (Somee) no soporta WebSocket, el cliente cae automáticamente a SSE/long-polling sobre 443.
/// </summary>
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationsHub : Hub
{
    private readonly ILogger<NotificationsHub> _logger;

    public NotificationsHub(ILogger<NotificationsHub> logger)
    {
        _logger = logger;
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
