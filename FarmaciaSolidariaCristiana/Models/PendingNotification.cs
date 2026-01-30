using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace FarmaciaSolidariaCristiana.Models;

/// <summary>
/// Modelo para almacenar notificaciones pendientes (sistema de polling)
/// Esto permite notificar a usuarios móviles sin depender de push notifications
/// </summary>
public class PendingNotification
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// ID del usuario destinatario
    /// </summary>
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Título de la notificación
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje/contenido de la notificación
    /// </summary>
    [Required]
    [StringLength(1000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Tipo de notificación (TurnoAprobado, TurnoRechazado, etc.)
    /// </summary>
    [Required]
    [StringLength(50)]
    public string NotificationType { get; set; } = string.Empty;

    /// <summary>
    /// ID de referencia (ej: TurnoId)
    /// </summary>
    public int? ReferenceId { get; set; }

    /// <summary>
    /// Tipo de referencia (ej: "Turno", "Entrega")
    /// </summary>
    [StringLength(50)]
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Datos adicionales en formato JSON
    /// </summary>
    public string? AdditionalData { get; set; }

    /// <summary>
    /// Indica si la notificación ha sido leída/mostrada
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Fecha de creación
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha en que fue leída (null si no ha sido leída)
    /// </summary>
    public DateTime? ReadAt { get; set; }

    // Navigation property - usando IdentityUser que es lo que usa la aplicación
    [ForeignKey("UserId")]
    public virtual IdentityUser? User { get; set; }
}

/// <summary>
/// Tipos de notificación soportados
/// </summary>
public static class NotificationTypes
{
    public const string TurnoSolicitado = "TurnoSolicitado";
    public const string TurnoAprobado = "TurnoAprobado";
    public const string TurnoRechazado = "TurnoRechazado";
    public const string TurnoEntregado = "TurnoEntregado";
    public const string TurnoCancelado = "TurnoCancelado";
    public const string General = "General";
}
