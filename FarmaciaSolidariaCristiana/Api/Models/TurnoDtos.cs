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
        public DateTime? FechaPreferida { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? NotasSolicitante { get; set; }
        public string? ComentariosFarmaceutico { get; set; }
        public DateTime? FechaRevision { get; set; }
        public List<TurnoMedicamentoDto> Medicamentos { get; set; } = new();
        public List<TurnoInsumoDto> Insumos { get; set; } = new();
        public int DocumentosCount { get; set; }
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
}
