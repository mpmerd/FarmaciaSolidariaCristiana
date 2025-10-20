using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models.ViewModels
{
    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [Display(Name = "Nombre de Usuario")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El correo electrónico es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido")]
        [Display(Name = "Correo Electrónico")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Contraseña")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden.")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Debe seleccionar un rol")]
        [Display(Name = "Rol")]
        public string Role { get; set; } = string.Empty;
    }
}
