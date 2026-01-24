using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models
{
    public class Patient
    {
        public int Id { get; set; }

        // CARNET DE IDENTIDAD O PASAPORTE (Obligatorio y único)
        [Required(ErrorMessage = "El Carnet de Identidad o Pasaporte es requerido")]
        [Display(Name = "Carnet de Identidad o Pasaporte")]
        [StringLength(20)]
        [RegularExpression(@"^(\d{11}|[A-Za-z]{1,3}\d{6,7})$", 
            ErrorMessage = "Formato inválido. Use 11 dígitos para Carnet de Identidad o 1-3 letras seguidas de 6-7 dígitos para Pasaporte")]
        public string IdentificationDocument { get; set; } = string.Empty;

        // 1. DATOS DEL PACIENTE
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [Display(Name = "Nombre Completo")]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La edad es requerida")]
        [Display(Name = "Edad")]
        [Range(0, 150)]
        public int Age { get; set; }

        [Required(ErrorMessage = "El sexo es requerido")]
        [Display(Name = "Sexo")]
        [StringLength(1)]
        public string Gender { get; set; } = string.Empty; // M o F

        [Display(Name = "Dirección")]
        [StringLength(500)]
        public string? Address { get; set; }

        [Display(Name = "Teléfono / Contacto")]
        [StringLength(50)]
        public string? Phone { get; set; }

        [Display(Name = "Municipio")]
        [StringLength(100)]
        public string? Municipality { get; set; }

        [Display(Name = "Provincia")]
        [StringLength(100)]
        public string? Province { get; set; }

        // 2. DATOS CLÍNICOS BÁSICOS
        [Display(Name = "Diagnóstico Principal")]
        [StringLength(500)]
        public string? MainDiagnosis { get; set; }

        [Display(Name = "Patologías Asociadas")]
        [StringLength(1000)]
        public string? AssociatedPathologies { get; set; }

        [Display(Name = "Alergias Conocidas")]
        [StringLength(500)]
        public string? KnownAllergies { get; set; }

        [Display(Name = "Tratamientos Actuales")]
        [StringLength(1000)]
        public string? CurrentTreatments { get; set; }

        [Display(Name = "Presión Arterial Sistólica")]
        public int? BloodPressureSystolic { get; set; }

        [Display(Name = "Presión Arterial Diastólica")]
        public int? BloodPressureDiastolic { get; set; }

        [Display(Name = "Peso (kg)")]
        [Range(0, 500)]
        public decimal? Weight { get; set; }

        [Display(Name = "Altura (cm)")]
        [Range(0, 300)]
        public decimal? Height { get; set; }

        // 6. OBSERVACIONES
        [Display(Name = "Observaciones / Notas")]
        [StringLength(2000)]
        public string? Observations { get; set; }

        // Metadatos
        [Display(Name = "Fecha de Registro")]
        public DateTime RegistrationDate { get; set; } = DateTime.Now;

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        // Relaciones
        public ICollection<Delivery> Deliveries { get; set; } = new List<Delivery>();
        public ICollection<PatientDocument> Documents { get; set; } = new List<PatientDocument>();
    }
}
