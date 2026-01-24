using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Api.Models
{
    /// <summary>
    /// DTO de donación para respuestas
    /// </summary>
    public class DonationDto
    {
        public int Id { get; set; }
        public int? MedicineId { get; set; }
        public string? MedicineName { get; set; }
        public int? SupplyId { get; set; }
        public string? SupplyName { get; set; }
        public int Quantity { get; set; }
        public DateTime DonationDate { get; set; }
        public string? DonorNote { get; set; }
        public string? Comments { get; set; }
    }

    /// <summary>
    /// DTO para crear una donación
    /// </summary>
    public class CreateDonationDto
    {
        public int? MedicineId { get; set; }
        public int? SupplyId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que 0")]
        public int Quantity { get; set; }

        public DateTime? DonationDate { get; set; }

        [StringLength(500, ErrorMessage = "La nota del donante no puede exceder 500 caracteres")]
        public string? DonorNote { get; set; }

        [StringLength(1000, ErrorMessage = "Los comentarios no pueden exceder 1000 caracteres")]
        public string? Comments { get; set; }
    }

    /// <summary>
    /// DTO de estadísticas de donaciones
    /// </summary>
    public class DonationStatsDto
    {
        public int TotalDonations { get; set; }
        public int TotalQuantity { get; set; }
        public int MedicineDonations { get; set; }
        public int SupplyDonations { get; set; }
    }
}
