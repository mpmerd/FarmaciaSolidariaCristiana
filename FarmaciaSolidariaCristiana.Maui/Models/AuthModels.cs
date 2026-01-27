namespace FarmaciaSolidariaCristiana.Maui.Models;

/// <summary>
/// Respuesta genérica de la API
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public DateTime Timestamp { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

/// <summary>
/// Respuesta de login
/// </summary>
public class LoginResponse
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime Expiration { get; set; }
    public UserInfo User { get; set; } = new();
}

/// <summary>
/// Información del usuario
/// </summary>
public class UserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    
    /// <summary>
    /// Obtiene el rol principal del usuario
    /// </summary>
    public string Role => Roles.FirstOrDefault() ?? string.Empty;
}

/// <summary>
/// Request de login
/// </summary>
public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>
/// Request de registro
/// </summary>
public class RegisterRequest
{
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
