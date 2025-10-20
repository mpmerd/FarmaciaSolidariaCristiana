using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models
{
    public class Donation
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un medicamento")]
        [Display(Name = "Medicamento")]
        public int MedicineId { get; set; }

        [Required(ErrorMessage = "La cantidad es obligatoria")]
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser mayor que 0")]
        [Display(Name = "Cantidad")]
        public int Quantity { get; set; }

        [Display(Name = "Fecha de Donación")]
        public DateTime DonationDate { get; set; } = DateTime.Now;

        [Display(Name = "Nota del Donante")]
        public string? DonorNote { get; set; }

        [Display(Name = "Comentarios")]
        public string? Comments { get; set; }

        // Navigation property
        public Medicine? Medicine { get; set; }
    }
}
