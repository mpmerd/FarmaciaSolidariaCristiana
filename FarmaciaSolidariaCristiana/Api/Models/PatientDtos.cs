using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Api.Models
{
    /// <summary>
    /// DTO de paciente para respuestas (resumen)
    /// </summary>
    public class PatientDto
    {
        public int Id { get; set; }
        public string IdentificationDocument { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Municipality { get; set; }
        public string? Province { get; set; }
        public string? MainDiagnosis { get; set; }
        public string? KnownAllergies { get; set; }
        public bool IsActive { get; set; }
        public DateTime RegistrationDate { get; set; }
        public int DeliveriesCount { get; set; }
    }

    /// <summary>
    /// DTO de paciente con todos los detalles
    /// </summary>
    public class PatientDetailDto : PatientDto
    {
        public string? AssociatedPathologies { get; set; }
        public string? CurrentTreatments { get; set; }
        public int? BloodPressureSystolic { get; set; }
        public int? BloodPressureDiastolic { get; set; }
        public decimal? Weight { get; set; }
        public decimal? Height { get; set; }
        public string? Observations { get; set; }
        public List<PatientDeliveryDto> RecentDeliveries { get; set; } = new();
        public int DocumentsCount { get; set; }
    }

    /// <summary>
    /// DTO de entrega en contexto de paciente
    /// </summary>
    public class PatientDeliveryDto
    {
        public int Id { get; set; }
        public string? MedicineName { get; set; }
        public int Quantity { get; set; }
        public DateTime DeliveryDate { get; set; }
    }

    /// <summary>
    /// DTO para crear un paciente
    /// </summary>
    public class CreatePatientDto
    {
        [Required(ErrorMessage = "El Carnet de Identidad o Pasaporte es requerido")]
        [StringLength(20)]
        [RegularExpression(@"^(\d{11}|[A-Za-z]{1,3}\d{6,7})$", 
            ErrorMessage = "Formato inválido. Use 11 dígitos para Carnet de Identidad o 1-3 letras seguidas de 6-7 dígitos para Pasaporte")]
        public string IdentificationDocument { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La edad es requerida")]
        [Range(0, 150)]
        public int Age { get; set; }

        [Required(ErrorMessage = "El sexo es requerido")]
        [StringLength(1)]
        [RegularExpression("^[MF]$", ErrorMessage = "El sexo debe ser M o F")]
        public string Gender { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Municipality { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(500)]
        public string? MainDiagnosis { get; set; }

        [StringLength(1000)]
        public string? AssociatedPathologies { get; set; }

        [StringLength(500)]
        public string? KnownAllergies { get; set; }

        [StringLength(1000)]
        public string? CurrentTreatments { get; set; }

        [StringLength(2000)]
        public string? Observations { get; set; }
    }

    /// <summary>
    /// DTO para actualizar un paciente
    /// </summary>
    public class UpdatePatientDto
    {
        [Required(ErrorMessage = "El nombre completo es requerido")]
        [StringLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "La edad es requerida")]
        [Range(0, 150)]
        public int Age { get; set; }

        [Required(ErrorMessage = "El sexo es requerido")]
        [StringLength(1)]
        public string Gender { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Municipality { get; set; }

        [StringLength(100)]
        public string? Province { get; set; }

        [StringLength(500)]
        public string? MainDiagnosis { get; set; }

        [StringLength(1000)]
        public string? AssociatedPathologies { get; set; }

        [StringLength(500)]
        public string? KnownAllergies { get; set; }

        [StringLength(1000)]
        public string? CurrentTreatments { get; set; }

        public int? BloodPressureSystolic { get; set; }
        public int? BloodPressureDiastolic { get; set; }

        [Range(0, 500)]
        public decimal? Weight { get; set; }

        [Range(0, 300)]
        public decimal? Height { get; set; }

        [StringLength(2000)]
        public string? Observations { get; set; }
    }

    /// <summary>
    /// DTO de documento de paciente
    /// </summary>
    public class PatientDocumentDto
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public string? Notes { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    /// <summary>
    /// DTO de estadísticas de pacientes
    /// </summary>
    public class PatientStatsDto
    {
        public int TotalPatients { get; set; }
        public int TotalInactive { get; set; }
        public int NewThisMonth { get; set; }
    }
}
