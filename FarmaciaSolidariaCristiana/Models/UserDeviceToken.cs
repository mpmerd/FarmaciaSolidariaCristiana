using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace FarmaciaSolidariaCristiana.Models
{
    /// <summary>
    /// Almacena los tokens de dispositivo OneSignal asociados a cada usuario.
    /// Un usuario puede tener múltiples dispositivos registrados.
    /// </summary>
    public class UserDeviceToken
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ID del usuario de Identity
        /// </summary>
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Navegación al usuario de Identity
        /// </summary>
        [ForeignKey(nameof(UserId))]
        public virtual IdentityUser? User { get; set; }

        /// <summary>
        /// Player ID de OneSignal (identificador único del dispositivo en OneSignal)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string OneSignalPlayerId { get; set; } = string.Empty;

        /// <summary>
        /// Token de push del dispositivo (opcional, para referencia)
        /// </summary>
        [StringLength(500)]
        public string? DeviceToken { get; set; }

        /// <summary>
        /// Tipo de dispositivo: "iOS", "Android", "Unknown"
        /// </summary>
        [StringLength(20)]
        public string DeviceType { get; set; } = "Unknown";

        /// <summary>
        /// Nombre o modelo del dispositivo (opcional)
        /// </summary>
        [StringLength(100)]
        public string? DeviceName { get; set; }

        /// <summary>
        /// Indica si el token está activo
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Fecha de registro del token
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Última vez que se actualizó el token
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Última vez que se usó este token para enviar una notificación
        /// </summary>
        public DateTime? LastUsedAt { get; set; }
    }
}
