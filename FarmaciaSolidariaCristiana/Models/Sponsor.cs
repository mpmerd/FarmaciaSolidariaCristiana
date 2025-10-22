using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models
{
    public class Sponsor
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del patrocinador es requerido")]
        [StringLength(200)]
        [Display(Name = "Nombre del Patrocinador")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [StringLength(500)]
        [Display(Name = "Logo")]
        public string? LogoPath { get; set; }

        [Display(Name = "Activo")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "Orden de Visualización")]
        public int DisplayOrder { get; set; } = 0;

        [Display(Name = "Fecha de Registro")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
