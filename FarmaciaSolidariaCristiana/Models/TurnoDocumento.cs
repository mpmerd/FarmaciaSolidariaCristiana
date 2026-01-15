using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models
{
    /// <summary>
    /// Representa un documento adjunto a una solicitud de turno.
    /// Permite subir múltiples documentos médicos (recetas, tarjetones, informes, etc.)
    /// </summary>
    public class TurnoDocumento
    {
        public int Id { get; set; }

        [Required]
        public int TurnoId { get; set; }

        [Required(ErrorMessage = "El tipo de documento es requerido")]
        [Display(Name = "Tipo de Documento")]
        [StringLength(100)]
        public string DocumentType { get; set; } = string.Empty;
        // Ejemplos: "Receta Médica", "Tarjetón Sanitario", "Informe Médico", "Otro"

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
        public Turno Turno { get; set; } = null!;
    }
}
