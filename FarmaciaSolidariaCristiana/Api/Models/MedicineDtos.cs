using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Api.Models
{
    /// <summary>
    /// DTO de medicamento para respuestas
    /// </summary>
    public class MedicineDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int StockQuantity { get; set; }
        public string Unit { get; set; } = "comprimidos";
        public string? NationalCode { get; set; }
    }

    /// <summary>
    /// DTO para crear un medicamento
    /// </summary>
    public class CreateMedicineDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a 0")]
        public int StockQuantity { get; set; } = 0;

        [StringLength(50, ErrorMessage = "La unidad no puede exceder 50 caracteres")]
        public string? Unit { get; set; }

        [StringLength(50, ErrorMessage = "El código nacional no puede exceder 50 caracteres")]
        public string? NationalCode { get; set; }
    }

    /// <summary>
    /// DTO para actualizar un medicamento
    /// </summary>
    public class UpdateMedicineDto
    {
        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(200, ErrorMessage = "El nombre no puede exceder 200 caracteres")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "La descripción no puede exceder 1000 caracteres")]
        public string? Description { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor o igual a 0")]
        public int StockQuantity { get; set; }

        [StringLength(50, ErrorMessage = "La unidad no puede exceder 50 caracteres")]
        public string? Unit { get; set; }

        [StringLength(50, ErrorMessage = "El código nacional no puede exceder 50 caracteres")]
        public string? NationalCode { get; set; }
    }

    /// <summary>
    /// DTO para resultado de búsqueda CIMA
    /// </summary>
    public class CimaSearchResultDto
    {
        public string NationalCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Resultado paginado genérico
    /// </summary>
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public bool HasPreviousPage => Page > 1;
        public bool HasNextPage => Page < TotalPages;
    }
}
