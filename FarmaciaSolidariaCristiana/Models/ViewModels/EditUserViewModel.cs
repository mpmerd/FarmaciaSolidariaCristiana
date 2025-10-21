using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Models.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [Display(Name = "Nombre de Usuario")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "Email inválido")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Rol Actual")]
        public string CurrentRole { get; set; } = string.Empty;

        [Display(Name = "Nuevo Rol")]
        public string? NewRole { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Nueva Contraseña (dejar vacío para no cambiar)")]
        public string? NewPassword { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirmar Nueva Contraseña")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string? ConfirmPassword { get; set; }
    }
}
