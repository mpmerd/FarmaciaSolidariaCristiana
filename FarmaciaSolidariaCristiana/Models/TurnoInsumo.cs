using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models
{
    /// <summary>
    /// Relación many-to-many entre Turnos e Insumos
    /// Permite solicitar múltiples insumos en un turno
    /// </summary>
    public class TurnoInsumo
    {
        public int Id { get; set; }

        [Required]
        public int TurnoId { get; set; }
        
        public Turno? Turno { get; set; }

        [Required]
        public int SupplyId { get; set; }
        
        public Supply? Supply { get; set; }

        /// <summary>
        /// Cantidad solicitada del insumo
        /// </summary>
        [Required]
        [Range(1, 1000)]
        public int CantidadSolicitada { get; set; }

        /// <summary>
        /// Indica si este insumo está disponible al momento de la solicitud
        /// </summary>
        public bool DisponibleAlSolicitar { get; set; } = true;

        /// <summary>
        /// Cantidad efectivamente aprobada por el farmacéutico
        /// </summary>
        [Range(0, 1000)]
        public int? CantidadAprobada { get; set; }

        /// <summary>
        /// Notas sobre este insumo específico
        /// </summary>
        [StringLength(500)]
        public string? Notas { get; set; }
    }
}
