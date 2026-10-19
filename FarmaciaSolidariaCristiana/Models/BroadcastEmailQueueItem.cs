using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace FarmaciaSolidariaCristiana.Models;

/// <summary>
/// Ítem de la cola persistente de emails de notificación masiva (broadcast).
/// La cola vive en la BD: sobrevive reciclados/reinicios del hosting (Somee).
/// La procesa BroadcastEmailProcessor (BackgroundService) a razón de
/// BroadcastEmailIntervalSeconds (default 60s) con tope diario
/// BroadcastEmailDailyMax (default 350) para respetar la cuota de Gmail.
/// </summary>
public class BroadcastEmailQueueItem
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Identificador de la campaña de broadcast (un Guid por envío masivo)
    /// </summary>
    [Required]
    public Guid BroadcastId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// ID del usuario destinatario (paciente ViewerPublic sin app activa)
    /// </summary>
    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Email del destinatario
    /// </summary>
    [Required]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Título del broadcast
    /// </summary>
    [Required]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Mensaje del broadcast
    /// </summary>
    [Required]
    [StringLength(2000)]
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Estado del ítem en la cola
    /// </summary>
    public BroadcastEmailStatus Status { get; set; } = BroadcastEmailStatus.Pending;

    /// <summary>
    /// Intentos de envío (3 fallos → Failed permanente)
    /// </summary>
    public int Attempts { get; set; } = 0;

    /// <summary>
    /// Fecha de creación (UTC)
    /// </summary>
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Fecha de envío exitoso (UTC; null si aún no se ha enviado)
    /// </summary>
    public DateTime? SentAtUtc { get; set; }

    /// <summary>
    /// Último error de envío (truncado a 500)
    /// </summary>
    [StringLength(500)]
    public string? LastError { get; set; }

    // Navigation property - mismo patrón que PendingNotification
    [ForeignKey("UserId")]
    public virtual IdentityUser? User { get; set; }
}

/// <summary>
/// Estados de un ítem de la cola de emails de broadcast
/// </summary>
public enum BroadcastEmailStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}
