using FarmaciaSolidariaCristiana.Maui.Models;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Interfaz para el servicio de autenticación
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Indica si el usuario está autenticado
    /// </summary>
    bool IsAuthenticated { get; }
    
    /// <summary>
    /// Verifica si el usuario está autenticado (async)
    /// </summary>
    Task<bool> IsAuthenticatedAsync();
    
    /// <summary>
    /// Obtiene el token JWT actual
    /// </summary>
    Task<string?> GetTokenAsync();
    
    /// <summary>
    /// Obtiene la información del usuario actual
    /// </summary>
    Task<UserInfo?> GetCurrentUserAsync();
    
    /// <summary>
    /// Obtiene la información del usuario actual (alias)
    /// </summary>
    Task<UserInfo?> GetUserInfoAsync();
    
    /// <summary>
    /// Verifica si el usuario tiene un rol específico
    /// </summary>
    Task<bool> IsInRoleAsync(string role);
    
    /// <summary>
    /// Verifica si el usuario tiene alguno de los roles especificados
    /// </summary>
    Task<bool> IsInAnyRoleAsync(params string[] roles);
    
    /// <summary>
    /// Inicia sesión
    /// </summary>
    Task<ApiResponse<LoginResponse>> LoginAsync(string email, string password);
    
    /// <summary>
    /// Cierra sesión
    /// </summary>
    Task LogoutAsync();
    
    /// <summary>
    /// Registra un nuevo usuario
    /// </summary>
    Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request);
    
    /// <summary>
    /// Refresca el token
    /// </summary>
    Task<bool> RefreshTokenAsync();
}
