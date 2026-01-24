using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Api.Models
{
    /// <summary>
    /// DTO de insumo para respuestas
    /// </summary>
    public class SupplyDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int StockQuantity { get; set; }
        public string Unit { get; set; } = "Unidades";
    }

    /// <summary>
    /// DTO para crear un insumo
    /// </summary>
    public class CreateSupplyDto
    {
        [Required(ErrorMessage = "El nombre del insumo es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a 0")]
        public int StockQuantity { get; set; } = 0;

        [StringLength(50, ErrorMessage = "La unidad no puede exceder 50 caracteres")]
        public string? Unit { get; set; }
    }

    /// <summary>
    /// DTO para actualizar un insumo
    /// </summary>
    public class UpdateSupplyDto
    {
        [Required(ErrorMessage = "El nombre del insumo es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a 0")]
        public int StockQuantity { get; set; }

        [StringLength(50, ErrorMessage = "La unidad no puede exceder 50 caracteres")]
        public string? Unit { get; set; }
    }
}
