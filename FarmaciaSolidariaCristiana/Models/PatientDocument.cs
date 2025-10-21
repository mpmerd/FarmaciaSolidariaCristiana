using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models
{
    public class PatientDocument
    {
        public int Id { get; set; }

        [Required]
        public int PatientId { get; set; }

        [Required(ErrorMessage = "El tipo de documento es requerido")]
        [Display(Name = "Tipo de Documento")]
        [StringLength(100)]
        public string DocumentType { get; set; } = string.Empty;
        // Ejemplos: "Receta Médica", "Documento de Identidad", "Evaluación Médica", "Consentimiento", "Tarjetón", "Tratamiento"

        [Required]
        [Display(Name = "Nombre del Archivo")]
        [StringLength(255)]
        public string FileName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Ruta del Archivo")]
        [StringLength(500)]
        public string FilePath { get; set; } = string.Empty;

        [Display(Name = "Tamaño del Archivo (bytes)")]
        public long FileSize { get; set; }

        [Display(Name = "Tipo MIME")]
        [StringLength(100)]
        public string? ContentType { get; set; }

        [Display(Name = "Descripción")]
        [StringLength(500)]
        public string? Description { get; set; }

        [Display(Name = "Fecha de Carga")]
        public DateTime UploadDate { get; set; } = DateTime.Now;

        // Relación
        public Patient Patient { get; set; } = null!;
    }
}
