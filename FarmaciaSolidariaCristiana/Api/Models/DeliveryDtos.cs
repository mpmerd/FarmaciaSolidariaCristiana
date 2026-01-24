using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Api.Models
{
    /// <summary>
    /// DTO de entrega para respuestas
    /// </summary>
    public class DeliveryDto
    {
        public int Id { get; set; }
        public string PatientIdentification { get; set; } = string.Empty;
        public int? PatientId { get; set; }
        public string? PatientName { get; set; }
        public int? MedicineId { get; set; }
        public string? MedicineName { get; set; }
        public int? SupplyId { get; set; }
        public string? SupplyName { get; set; }
        public int? TurnoId { get; set; }
        public int Quantity { get; set; }
        public DateTime DeliveryDate { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string? Dosage { get; set; }
        public string? TreatmentDuration { get; set; }
        public string? BatchNumber { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? DeliveredBy { get; set; }
        public string? Comments { get; set; }
    }

    /// <summary>
    /// DTO para crear una entrega
    /// </summary>
    public class CreateDeliveryDto
    {
        [Required(ErrorMessage = "El Carnet de Identidad o Pasaporte es requerido")]
        [StringLength(20)]
        [RegularExpression(@"^(\d{11}|[A-Za-z]{1,3}\d{6,7})$", 
            ErrorMessage = "Formato inválido. Use 11 dígitos para Carnet de Identidad o 1-3 letras seguidas de 6-7 dígitos para Pasaporte")]
        public string PatientIdentification { get; set; } = string.Empty;

        public int? PatientId { get; set; }
        public int? MedicineId { get; set; }
        public int? SupplyId { get; set; }
        public int? TurnoId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que 0")]
        public int Quantity { get; set; }

        public DateTime? DeliveryDate { get; set; }

        [StringLength(100)]
        public string? Dosage { get; set; }

        [StringLength(100)]
        public string? TreatmentDuration { get; set; }

        [StringLength(50)]
        public string? BatchNumber { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [StringLength(200)]
        public string? DeliveredBy { get; set; }

        [StringLength(500)]
        public string? PatientNote { get; set; }

        [StringLength(1000)]
        public string? Comments { get; set; }
    }

    /// <summary>
    /// DTO de estadísticas de entregas
    /// </summary>
    public class DeliveryStatsDto
    {
        public int TotalDeliveries { get; set; }
        public int TotalQuantity { get; set; }
        public int MedicineDeliveries { get; set; }
        public int SupplyDeliveries { get; set; }
        public int UniquePatients { get; set; }
    }
}
