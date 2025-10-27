using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models
{
    public class Supply
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "El nombre del insumo es obligatorio")]
        [Display(Name = "Nombre del Insumo")]
        public string Name { get; set; } = string.Empty;

        [Display(Name = "Descripción")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "La cantidad en stock es obligatoria")]
        [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a 0")]
        [Display(Name = "Cantidad en Stock")]
        public int StockQuantity { get; set; }

        [Display(Name = "Unidad")]
        public string Unit { get; set; } = "Unidades";
    }
}
