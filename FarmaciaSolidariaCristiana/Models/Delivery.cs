using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models
{
    public class Delivery
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El Carnet de Identidad o Pasaporte es requerido")]
        [Display(Name = "Carnet de Identidad o Pasaporte")]
        [StringLength(20)]
        [RegularExpression(@"^(\d{11}|[A-Za-z]\d{6,7})$", 
            ErrorMessage = "Formato inválido. Use 11 dígitos para Carnet de Identidad o letra seguida de 6-7 dígitos para Pasaporte")]
        public string PatientIdentification { get; set; } = string.Empty;

        [Display(Name = "Medicamento")]
        public int? MedicineId { get; set; }

        [Display(Name = "Insumo")]
        public int? SupplyId { get; set; }

        [Display(Name = "Paciente")]
        public int? PatientId { get; set; }

        [Display(Name = "Turno")]
        public int? TurnoId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que 0")]
        [Display(Name = "Cantidad")]
        public int Quantity { get; set; }

        [Display(Name = "Fecha de Entrega")]
        public DateTime DeliveryDate { get; set; } = DateTime.Now;

        [Display(Name = "Fecha de Creación")]
        public DateTime? CreatedAt { get; set; }

        [Display(Name = "Nota del Paciente")]
        public string? PatientNote { get; set; }

        [Display(Name = "Comentarios Generales")]
        public string? Comments { get; set; }

        // 3. MEDICAMENTO SOLICITADO - Información adicional
        [Display(Name = "Dosis")]
        [StringLength(100)]
        public string? Dosage { get; set; }

        [Display(Name = "Duración del Tratamiento")]
        [StringLength(100)]
        public string? TreatmentDuration { get; set; }

        // 5. ENTREGA DEL MEDICAMENTO
        [Display(Name = "Lote")]
        [StringLength(50)]
        public string? BatchNumber { get; set; }

        [Display(Name = "Fecha de Vencimiento")]
        [DataType(DataType.Date)]
        public DateTime? ExpiryDate { get; set; }

        [Display(Name = "Entregado Por")]
        [StringLength(200)]
        public string? DeliveredBy { get; set; }

        // Navigation properties
        public Medicine? Medicine { get; set; }
        public Supply? Supply { get; set; }
        public Patient? Patient { get; set; }
        public Turno? Turno { get; set; }
    }
}
