using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Api.Models
{
    /// <summary>
    /// DTO de patrocinador para respuestas
    /// </summary>
    public class SponsorDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LogoPath { get; set; }
        public bool IsActive { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    /// <summary>
    /// DTO para crear un patrocinador
    /// </summary>
    public class CreateSponsorDto
    {
        [Required(ErrorMessage = "El nombre del patrocinador es requerido")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// DTO para actualizar un patrocinador
    /// </summary>
    public class UpdateSponsorDto
    {
        [Required(ErrorMessage = "El nombre del patrocinador es requerido")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }

        public int DisplayOrder { get; set; }
    }
}
