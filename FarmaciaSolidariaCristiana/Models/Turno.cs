using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace FarmaciaSolidariaCristiana.Models
{
    /// <summary>
    /// Representa una solicitud de turno para retirar medicamentos en la farmacia.
    /// Sistema anti-abuso: 2 turnos por mes por usuario, validación manual de documentos.
    /// </summary>
    public class Turno
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        
        /// <summary>
        /// Usuario que solicita el turno (ViewerPublic)
        /// </summary>
        public IdentityUser? User { get; set; }

        /// <summary>
        /// Número de documento de identidad (carnet/pasaporte) - HASHEADO
        /// </summary>
        [Required]
        [StringLength(100)]
        public string DocumentoIdentidadHash { get; set; } = string.Empty;

        /// <summary>
        /// Medicamentos solicitados en este turno
        /// </summary>
        public ICollection<TurnoMedicamento> Medicamentos { get; set; } = new List<TurnoMedicamento>();

        /// <summary>
        /// Insumos médicos solicitados en este turno
        /// </summary>
        public ICollection<TurnoInsumo> Insumos { get; set; } = new List<TurnoInsumo>();

        /// <summary>
        /// Documentos adjuntos a este turno (recetas, tarjetones, informes, etc.)
        /// </summary>
        public ICollection<TurnoDocumento> Documentos { get; set; } = new List<TurnoDocumento>();

        /// <summary>
        /// Fecha y hora asignada automáticamente para el turno (se asigna al aprobar)
        /// Sistema: Martes/Jueves 1-4 PM, slots cada 6 minutos (30 turnos/día)
        /// </summary>
        public DateTime? FechaPreferida { get; set; }

        /// <summary>
        /// Fecha y hora de creación de la solicitud
        /// </summary>
        [Required]
        public DateTime FechaSolicitud { get; set; } = DateTime.Now;

        /// <summary>
        /// Estado actual del turno: Pendiente, Aprobado, Rechazado, Completado, Cancelado
        /// </summary>
        [Required]
        [StringLength(20)]
        public string Estado { get; set; } = "Pendiente";

        /// <summary>
        /// Ruta del archivo de receta médica (si aplica)
        /// </summary>
        [StringLength(500)]
        public string? RecetaMedicaPath { get; set; }

        /// <summary>
        /// Ruta del archivo de tarjetón/documento de identidad
        /// </summary>
        [StringLength(500)]
        public string? TarjetonPath { get; set; }

        /// <summary>
        /// Notas adicionales del solicitante
        /// </summary>
        [StringLength(1000)]
        public string? NotasSolicitante { get; set; }

        /// <summary>
        /// Comentarios del farmacéutico al revisar
        /// </summary>
        [StringLength(1000)]
        public string? ComentariosFarmaceutico { get; set; }

        /// <summary>
        /// ID del farmacéutico que revisó (Admin o Farmaceutico)
        /// </summary>
        [StringLength(450)]
        public string? RevisadoPorId { get; set; }

        /// <summary>
        /// Usuario farmacéutico que revisó
        /// </summary>
        public IdentityUser? RevisadoPor { get; set; }

        /// <summary>
        /// Fecha de revisión (aprobación/rechazo)
        /// </summary>
        public DateTime? FechaRevision { get; set; }

        /// <summary>
        /// Fecha de entrega efectiva (cuando se completó)
        /// </summary>
        public DateTime? FechaEntrega { get; set; }

        /// <summary>
        /// Ruta del PDF generado al aprobar
        /// </summary>
        [StringLength(500)]
        public string? TurnoPdfPath { get; set; }

        /// <summary>
        /// Número de turno único para el día
        /// </summary>
        public int? NumeroTurno { get; set; }

        /// <summary>
        /// Email de notificación enviado
        /// </summary>
        public bool EmailEnviado { get; set; } = false;
    }

    /// <summary>
    /// Estados posibles de un turno
    /// </summary>
    public static class EstadoTurno
    {
        public const string Pendiente = "Pendiente";
        public const string Aprobado = "Aprobado";
        public const string Rechazado = "Rechazado";
        public const string Completado = "Completado";
        public const string Cancelado = "Cancelado";
    }
}
