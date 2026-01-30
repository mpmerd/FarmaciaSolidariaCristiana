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
}

/// <summary>
/// Implementación del servicio de notificaciones pendientes
/// </summary>
public class PendingNotificationService : IPendingNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PendingNotificationService> _logger;

    public PendingNotificationService(
        ApplicationDbContext context,
        ILogger<PendingNotificationService> logger)
    {
        _context = context;
        _logger = logger;
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

        return notification;
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
}
