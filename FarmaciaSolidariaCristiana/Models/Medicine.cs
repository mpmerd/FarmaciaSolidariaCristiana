using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models
{
    public class Medicine
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [Display(Name = "Nombre del Medicamento")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [Display(Name = "Cantidad en Stock")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a 0")]
        public int StockQuantity { get; set; } = 0;

        [Display(Name = "Unidad")]
        public string Unit { get; set; } = "comprimidos";

        [Display(Name = "Código Nacional (CN)")]
        public string? NationalCode { get; set; }

        // Navigation properties
        public ICollection<Delivery>? Deliveries { get; set; }
        public ICollection<Donation>? Donations { get; set; }
    }
}
