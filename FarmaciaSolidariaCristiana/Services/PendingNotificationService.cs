using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FarmaciaSolidariaCristiana.Services;

/// <summary>
/// Interfaz para el servicio de notificaciones pendientes (polling)
/// </summary>
public interface IPendingNotificationService
{
    /// <summary>
    /// Crea una notificación pendiente para un usuario
    /// </summary>
    Task<PendingNotification> CreateNotificationAsync(
        string userId,
        string title,
        string message,
        string notificationType,
        int? referenceId = null,
        string? referenceType = null,
        object? additionalData = null);

    /// <summary>
    /// Crea notificaciones pendientes para múltiples usuarios en una sola operación:
    /// bulk insert (1 SaveChanges) + fan-out SignalR en paralelo por chunks.
    /// Evita el loop secuencial de <see cref="CreateNotificationAsync"/> (que hace
    /// 1 SaveChanges + 1 send por usuario) y previene timeout de Somee para N grande.
    /// Usar para notificaciones masivas/broadcast.
    /// </summary>
    Task<List<PendingNotification>> CreateBulkNotificationsAsync(
        IEnumerable<string> userIds,
        string title,
        string message,
        string notificationType,
        int? referenceId = null,
        string? referenceType = null,
        object? additionalData = null);

    /// <summary>
    /// Obtiene las notificaciones no leídas de un usuario
    /// </summary>
    Task<List<PendingNotification>> GetUnreadNotificationsAsync(string userId);

    /// <summary>
    /// Obtiene todas las notificaciones de un usuario (paginadas)
    /// </summary>
    Task<List<PendingNotification>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 20);

    /// <summary>
    /// Marca una notificación como leída
    /// </summary>
    Task<bool> MarkAsReadAsync(int notificationId, string userId);

    /// <summary>
    /// Marca todas las notificaciones de un usuario como leídas
    /// </summary>
    Task<int> MarkAllAsReadAsync(string userId);

    /// <summary>
    /// Obtiene el conteo de notificaciones no leídas
    /// </summary>
    Task<int> GetUnreadCountAsync(string userId);

    /// <summary>
    /// Verifica si un usuario está activo en la app móvil (tiene registro reciente)
    /// </summary>
    Task<bool> IsUserActiveOnMobileAsync(string userId);

    /// <summary>
    /// Limpia notificaciones antiguas (más de 30 días)
    /// </summary>
    Task<int> CleanupOldNotificationsAsync(int daysToKeep = 30);

    /// <summary>
    /// Marca como leídas todas las notificaciones relacionadas con un turno específico.
    /// Usado cuando un turno cambia de estado para evitar notificaciones obsoletas.
    /// </summary>
    Task<int> MarkNotificationsAsReadByReferenceAsync(int referenceId, string referenceType);
}

/// <summary>
/// Implementación del servicio de notificaciones pendientes
/// </summary>
public class PendingNotificationService : IPendingNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PendingNotificationService> _logger;
    private readonly INotificationBroadcaster? _broadcaster;

    public PendingNotificationService(
        ApplicationDbContext context,
        ILogger<PendingNotificationService> logger)
    {
        _context = context;
        _logger = logger;
        _broadcaster = null; // sin broadcast (compat retro)
    }

    public PendingNotificationService(
        ApplicationDbContext context,
        ILogger<PendingNotificationService> logger,
        INotificationBroadcaster broadcaster)
    {
        _context = context;
        _logger = logger;
        _broadcaster = broadcaster;
    }

    public async Task<PendingNotification> CreateNotificationAsync(
        string userId,
        string title,
        string message,
        string notificationType,
        int? referenceId = null,
        string? referenceType = null,
        object? additionalData = null)
    {
        var notification = new PendingNotification
        {
            UserId = userId,
            Title = title,
            Message = message,
            NotificationType = notificationType,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            AdditionalData = additionalData != null ? JsonSerializer.Serialize(additionalData) : null,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.PendingNotifications.Add(notification);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Notificación creada para usuario {UserId}: {Title} (Tipo: {Type})",
            userId, title, notificationType);

        // Fase 2: difundir en tiempo real por SignalR (canal sobre 443).
        // Si el usuario tiene una conexión activa, recibe el "push real" al instante.
        // Si no, la notificación queda pendiente y la recogerá el polling (fallback).
        if (_broadcaster != null)
        {
            try
            {
                await _broadcaster.BroadcastToUserAsync(userId, new NotificationPayload
                {
                    Id = notification.Id,
                    Title = notification.Title,
                    Message = notification.Message,
                    NotificationType = notification.NotificationType,
                    ReferenceId = notification.ReferenceId,
                    ReferenceType = notification.ReferenceType,
                    CreatedAt = notification.CreatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error difundiendo notificación {Id} por SignalR", notification.Id);
            }
        }

        return notification;
    }

    /// <inheritdoc/>
    public async Task<List<PendingNotification>> CreateBulkNotificationsAsync(
        IEnumerable<string> userIds,
        string title,
        string message,
        string notificationType,
        int? referenceId = null,
        string? referenceType = null,
        object? additionalData = null)
    {
        var now = DateTime.UtcNow;
        var data = additionalData != null ? JsonSerializer.Serialize(additionalData) : null;
        var targetIds = userIds.Distinct().ToList();

        if (targetIds.Count == 0)
            return new List<PendingNotification>();

        var notifications = targetIds.Select(userId => new PendingNotification
        {
            UserId = userId,
            Title = title,
            Message = message,
            NotificationType = notificationType,
            ReferenceId = referenceId,
            ReferenceType = referenceType,
            AdditionalData = data,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        // 1 solo SaveChanges para todos (bulk insert). Evita N round-trips a la BD.
        await _context.PendingNotifications.AddRangeAsync(notifications);
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Notificaciones masivas creadas: {Count} para '{Title}' (Tipo: {Type})",
            notifications.Count, title, notificationType);

        // Fan-out SignalR en paralelo por chunks de 100: no encadena 1 send tras otro
        // por usuario. Si el usuario no está conectado, el send es no-op (barato); la
        // notificación ya quedó en BD para polling/catch-up.
        if (_broadcaster != null)
        {
            var broadcaster = _broadcaster;
            const int chunkSize = 100;
            for (int i = 0; i < notifications.Count; i += chunkSize)
            {
                var chunk = notifications.Skip(i).Take(chunkSize).ToList();
                await Task.WhenAll(chunk.Select(n => broadcaster.BroadcastToUserAsync(
                    n.UserId,
                    new NotificationPayload
                    {
                        Id = n.Id,
                        Title = n.Title,
                        Message = n.Message,
                        NotificationType = n.NotificationType,
                        ReferenceId = n.ReferenceId,
                        ReferenceType = n.ReferenceType,
                        CreatedAt = n.CreatedAt
                    })));
            }
        }

        return notifications;
    }

    public async Task<List<PendingNotification>> GetUnreadNotificationsAsync(string userId)
    {
        return await _context.PendingNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PendingNotification>> GetNotificationsAsync(string userId, int page = 1, int pageSize = 20)
    {
        return await _context.PendingNotifications
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<bool> MarkAsReadAsync(int notificationId, string userId)
    {
        var notification = await _context.PendingNotifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification == null)
            return false;

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(string userId)
    {
        var now = DateTime.UtcNow;
        var count = await _context.PendingNotifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        _logger.LogInformation("Marcadas {Count} notificaciones como leídas para usuario {UserId}", count, userId);
        return count;
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.PendingNotifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool> IsUserActiveOnMobileAsync(string userId)
    {
        // Un usuario se considera activo en móvil si tiene un dispositivo registrado
        // que fue actualizado en las últimas 24 horas
        var cutoff = DateTime.UtcNow.AddHours(-24);
        
        return await _context.UserDeviceTokens
            .AnyAsync(d => d.UserId == userId && d.IsActive && d.UpdatedAt >= cutoff);
    }

    public async Task<int> CleanupOldNotificationsAsync(int daysToKeep = 30)
    {
        var cutoff = DateTime.UtcNow.AddDays(-daysToKeep);
        
        var count = await _context.PendingNotifications
            .Where(n => n.CreatedAt < cutoff)
            .ExecuteDeleteAsync();

        if (count > 0)
        {
            _logger.LogInformation("Eliminadas {Count} notificaciones antiguas (más de {Days} días)", count, daysToKeep);
        }

        return count;
    }

    public async Task<int> MarkNotificationsAsReadByReferenceAsync(int referenceId, string referenceType)
    {
        var now = DateTime.UtcNow;
        var count = await _context.PendingNotifications
            .Where(n => n.ReferenceId == referenceId && 
                       n.ReferenceType == referenceType && 
                       !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        if (count > 0)
        {
            _logger.LogInformation(
                "Marcadas {Count} notificaciones como leídas para {Type} #{Id}",
                count, referenceType, referenceId);
        }

        return count;
    }
}
