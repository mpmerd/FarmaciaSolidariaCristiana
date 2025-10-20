using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [Display(Name = "Nombre de Usuario")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Recordarme")]
        public bool RememberMe { get; set; }
    }
}
