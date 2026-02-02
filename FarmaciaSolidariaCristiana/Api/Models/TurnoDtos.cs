using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Api.Models
{
    /// <summary>
    /// DTO de turno para respuestas
    /// </summary>
    public class TurnoDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public int? NumeroTurno { get; set; }
        public DateTime? FechaPreferida { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? NotasSolicitante { get; set; }
        public string? ComentariosFarmaceutico { get; set; }
        public DateTime? FechaRevision { get; set; }
        public string? TurnoPdfPath { get; set; }
        public List<TurnoMedicamentoDto> Medicamentos { get; set; } = new();
        public List<TurnoInsumoDto> Insumos { get; set; } = new();
        public int DocumentosCount { get; set; }
        public List<TurnoDocumentoDto> Documentos { get; set; } = new();
        
        /// <summary>
        /// Indica si el turno fue cancelado por no presentación del paciente.
        /// </summary>
        public bool CanceladoPorNoPresentacion { get; set; }
    }

    /// <summary>
    /// DTO de medicamento en turno
    /// </summary>
    public class TurnoMedicamentoDto
    {
        public int MedicineId { get; set; }
        public string MedicineName { get; set; } = string.Empty;
        public int CantidadSolicitada { get; set; }
        public int? CantidadAprobada { get; set; }
        public bool DisponibleAlSolicitar { get; set; }
    }

    /// <summary>
    /// DTO de insumo en turno
    /// </summary>
    public class TurnoInsumoDto
    {
        public int SupplyId { get; set; }
        public string SupplyName { get; set; } = string.Empty;
        public int CantidadSolicitada { get; set; }
        public int? CantidadAprobada { get; set; }
        public bool DisponibleAlSolicitar { get; set; }
    }

    /// <summary>
    /// DTO para verificar si puede solicitar turno
    /// </summary>
    public class CanRequestTurnoDto
    {
        public bool CanRequest { get; set; }
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO para aprobar un turno
    /// </summary>
    public class ApproveTurnoDto
    {
        [StringLength(1000)]
        public string? Comentarios { get; set; }
    }

    /// <summary>
    /// DTO para rechazar un turno
    /// </summary>
    public class RejectTurnoDto
    {
        [Required(ErrorMessage = "El motivo de rechazo es requerido")]
        [StringLength(1000, ErrorMessage = "El motivo no puede exceder 1000 caracteres")]
        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para reprogramar un turno
    /// </summary>
    public class RescheduleTurnoDto
    {
        [Required(ErrorMessage = "La nueva fecha es requerida")]
        public DateTime NuevaFecha { get; set; }
        
        [StringLength(500)]
        public string? Motivo { get; set; }
    }

    /// <summary>
    /// DTO de estadísticas de turnos
    /// </summary>
    public class TurnoStatsDto
    {
        public int TotalPendientes { get; set; }
        public int TotalAprobados { get; set; }
        public int TotalCompletados { get; set; }
        public int TotalRechazados { get; set; }
        public int TurnosHoy { get; set; }
        public int TurnosEsteMes { get; set; }
    }

    /// <summary>
    /// DTO para crear una solicitud de turno desde la app móvil
    /// </summary>
    public class CreateTurnoApiDto
    {
        [Required(ErrorMessage = "El documento de identidad es requerido")]
        [StringLength(20)]
        [RegularExpression(@"^(\d{11}|[A-Za-z]{1,3}\d{6,7})$", 
            ErrorMessage = "Formato inválido. Use 11 dígitos para CI o 1-3 letras + 6-7 dígitos para Pasaporte")]
        public string DocumentoIdentidad { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Notas { get; set; }

        /// <summary>
        /// Tipo de solicitud: "Medicamento" o "Insumo"
        /// </summary>
        [Required(ErrorMessage = "El tipo de solicitud es requerido")]
        public string TipoSolicitud { get; set; } = "Medicamento";

        /// <summary>
        /// Lista de items (medicamentos o insumos según TipoSolicitud)
        /// </summary>
        public List<TurnoItemDto> Items { get; set; } = new();
    }

    /// <summary>
    /// Item de turno (medicamento o insumo)
    /// </summary>
    public class TurnoItemDto
    {
        public int Id { get; set; }
        public int Cantidad { get; set; } = 1;
    }

    /// <summary>
    /// DTO para documento de turno
    /// </summary>
    public class TurnoDocumentoDto
    {
        public int Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string? ContentType { get; set; }
        public string? Description { get; set; }
        public DateTime UploadDate { get; set; }
    }

    /// <summary>
    /// DTO para cancelar un turno por el paciente
    /// </summary>
    public class CancelTurnoDto
    {
        [Required(ErrorMessage = "Debe proporcionar un motivo de cancelación")]
        [StringLength(500, ErrorMessage = "El motivo no puede exceder 500 caracteres")]
        public string Motivo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para la respuesta de si se puede cancelar un turno
    /// </summary>
    public class CanCancelTurnoDto
    {
        public bool CanCancel { get; set; }
        public string? Reason { get; set; }
        public int DiasRestantes { get; set; }
    }

    /// <summary>
    /// DTO de turno simplificado para entregas
    /// </summary>
    public class TurnoForDeliveryDto
    {
        public int Id { get; set; }
        public int? NumeroTurno { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaPreferida { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public List<TurnoItemForDeliveryDto> Medicamentos { get; set; } = new();
        public List<TurnoItemForDeliveryDto> Insumos { get; set; } = new();
    }

    /// <summary>
    /// DTO de item de turno para entregas (con stock actual)
    /// </summary>
    public class TurnoItemForDeliveryDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int CantidadSolicitada { get; set; }
        public int? CantidadAprobada { get; set; }
        public string Unidad { get; set; } = string.Empty;
        public int StockActual { get; set; }
        public string Tipo { get; set; } = string.Empty; // "Medicamento" o "Insumo"
    }
}
