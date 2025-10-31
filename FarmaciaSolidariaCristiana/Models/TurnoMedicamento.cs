using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models
{
    /// <summary>
    /// Relación many-to-many entre Turnos y Medicamentos
    /// Permite solicitar múltiples medicamentos en un turno
    /// </summary>
    public class TurnoMedicamento
    {
        public int Id { get; set; }

        [Required]
        public int TurnoId { get; set; }
        
        public Turno? Turno { get; set; }

        [Required]
        public int MedicineId { get; set; }
        
        public Medicine? Medicine { get; set; }

        /// <summary>
        /// Cantidad solicitada del medicamento
        /// </summary>
        [Required]
        [Range(1, 1000)]
        public int CantidadSolicitada { get; set; }

        /// <summary>
        /// Indica si este medicamento está disponible al momento de la solicitud
        /// </summary>
        public bool DisponibleAlSolicitar { get; set; } = true;

        /// <summary>
        /// Cantidad efectivamente aprobada por el farmacéutico
        /// </summary>
        [Range(0, 1000)]
        public int? CantidadAprobada { get; set; }

        /// <summary>
        /// Notas sobre este medicamento específico
        /// </summary>
        [StringLength(500)]
        public string? Notas { get; set; }
    }
}
