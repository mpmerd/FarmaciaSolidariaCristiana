using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Api.Models
{
    // ========================================
    // DTOs para registro de dispositivos
    // ========================================

    /// <summary>
    /// DTO para registrar un token de dispositivo OneSignal
    /// </summary>
    public class RegisterDeviceTokenDto
    {
        /// <summary>
        /// Player ID de OneSignal (requerido)
        /// </summary>
        [Required(ErrorMessage = "El OneSignalPlayerId es requerido")]
        [StringLength(100, ErrorMessage = "El OneSignalPlayerId no puede exceder 100 caracteres")]
        public string OneSignalPlayerId { get; set; } = string.Empty;

        /// <summary>
        /// Token de push del dispositivo (opcional)
        /// </summary>
        [StringLength(500)]
        public string? DeviceToken { get; set; }

        /// <summary>
        /// Tipo de dispositivo: "iOS", "Android"
        /// </summary>
        [StringLength(20)]
        public string DeviceType { get; set; } = "Unknown";

        /// <summary>
        /// Nombre o modelo del dispositivo
        /// </summary>
        [StringLength(100)]
        public string? DeviceName { get; set; }
    }

    /// <summary>
    /// DTO para eliminar un token de dispositivo
    /// </summary>
    public class UnregisterDeviceTokenDto
    {
        [Required(ErrorMessage = "El OneSignalPlayerId es requerido")]
        public string OneSignalPlayerId { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO de respuesta con información del dispositivo registrado
    /// </summary>
    public class DeviceTokenResponseDto
    {
        public int Id { get; set; }
        public string OneSignalPlayerId { get; set; } = string.Empty;
        public string DeviceType { get; set; } = string.Empty;
        public string? DeviceName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    // ========================================
    // DTOs para envío de notificaciones
    // ========================================

    /// <summary>
    /// Tipos de notificación soportados
    /// </summary>
    public enum NotificationType
    {
        /// <summary>Turno solicitado por el usuario</summary>
        TurnoSolicitado,
        
        /// <summary>Turno aprobado por farmacéutico</summary>
        TurnoAprobado,
        
        /// <summary>Turno rechazado por farmacéutico</summary>
        TurnoRechazado,
        
        /// <summary>PDF del turno disponible para descarga</summary>
        TurnoPdfDisponible,
        
        /// <summary>Recordatorio de fecha/hora del turno</summary>
        TurnoRecordatorio,
        
        /// <summary>Turno cancelado</summary>
        TurnoCancelado,
        
        /// <summary>Turno reprogramado</summary>
        TurnoReprogramado,
        
        /// <summary>Notificación general/informativa</summary>
        General
    }

    /// <summary>
    /// DTO para enviar una notificación push
    /// </summary>
    public class SendNotificationDto
    {
        /// <summary>
        /// ID del usuario destino (opcional si se especifican PlayerIds)
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Lista de Player IDs de OneSignal destino (opcional si se especifica UserId)
        /// </summary>
        public List<string>? PlayerIds { get; set; }

        /// <summary>
        /// Título de la notificación
        /// </summary>
        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(100, ErrorMessage = "El título no puede exceder 100 caracteres")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Mensaje de la notificación
        /// </summary>
        [Required(ErrorMessage = "El mensaje es requerido")]
        [StringLength(500, ErrorMessage = "El mensaje no puede exceder 500 caracteres")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Tipo de notificación
        /// </summary>
        public NotificationType Type { get; set; } = NotificationType.General;

        /// <summary>
        /// Datos adicionales para la notificación (JSON serializable)
        /// </summary>
        public Dictionary<string, string>? Data { get; set; }
    }

    /// <summary>
    /// DTO de respuesta al enviar una notificación
    /// </summary>
    public class NotificationResultDto
    {
        public bool Success { get; set; }
        public string? NotificationId { get; set; }
        public int RecipientsCount { get; set; }
        public string? ErrorMessage { get; set; }
    }

    // ========================================
    // DTOs específicos para notificaciones de turno
    // ========================================

    /// <summary>
    /// DTO para notificación de turno solicitado
    /// </summary>
    public class TurnoNotificationDataDto
    {
        public int TurnoId { get; set; }
        public int NumeroTurno { get; set; }
        public string? FechaTurno { get; set; }
        public string? HoraTurno { get; set; }
        public string? Estado { get; set; }
        public string? PdfUrl { get; set; }
        public string? Motivo { get; set; }
    }

    /// <summary>
    /// Configuración de notificaciones del usuario
    /// </summary>
    public class UserNotificationPreferencesDto
    {
        public bool PushEnabled { get; set; } = true;
        public bool EmailEnabled { get; set; } = true;
        public bool TurnoSolicitadoEnabled { get; set; } = true;
        public bool TurnoAprobadoEnabled { get; set; } = true;
        public bool TurnoRechazadoEnabled { get; set; } = true;
        public bool RecordatoriosEnabled { get; set; } = true;
    }

    // ========================================
    // DTOs para OneSignal API Responses
    // ========================================

    /// <summary>
    /// Respuesta de la API de OneSignal al crear una notificación
    /// </summary>
    public class OneSignalNotificationResponse
    {
        public string? Id { get; set; }
        public int Recipients { get; set; }
        public string? External_id { get; set; }
        public List<string>? Errors { get; set; }
    }
}
