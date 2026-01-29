using System.ComponentModel.DataAnnotations;

namespace FarmaciaSolidariaCristiana.Api.Models
{
    /// <summary>
    /// DTO para solicitud de login
    /// </summary>
    public class LoginRequestDto
    {
        /// <summary>
        /// Email o nombre de usuario
        /// </summary>
        [Required(ErrorMessage = "El email o usuario es requerido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para respuesta de login exitoso
    /// </summary>
    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
        public UserDto User { get; set; } = null!;
    }

    /// <summary>
    /// DTO con información del usuario
    /// </summary>
    public class UserDto
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }

    /// <summary>
    /// DTO para cambio de contraseña
    /// </summary>
    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "La contraseña actual es requerida")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva contraseña es requerida")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        [Compare("NewPassword", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para registro de nuevo usuario
    /// </summary>
    public class RegisterRequestDto
    {
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "El nombre de usuario debe tener entre 3 y 50 caracteres")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El email es requerido")]
        [EmailAddress(ErrorMessage = "El formato de email no es válido")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "La contraseña es requerida")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "La contraseña debe tener entre 6 y 100 caracteres")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmación de contraseña es requerida")]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para solicitar recuperación de contraseña
    /// </summary>
    public class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "El email o nombre de usuario es requerido")]
        public string EmailOrUserName { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO para verificar si el registro está habilitado
    /// </summary>
    public class RegistrationStatusDto
    {
        public bool IsEnabled { get; set; }
        public string? Message { get; set; }
    }
}
