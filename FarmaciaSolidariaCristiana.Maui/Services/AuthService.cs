using System.Net.Http.Json;
using System.Text.Json;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Implementación del servicio de autenticación
/// </summary>
public class AuthService : IAuthService
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private UserInfo? _currentUser;
    private bool _isAuthenticated;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public bool IsAuthenticated => _isAuthenticated;

    public async Task<string?> GetTokenAsync()
    {
        try
        {
            var token = await SecureStorage.GetAsync(Constants.AuthTokenKey);
            
            if (string.IsNullOrEmpty(token))
            {
                _isAuthenticated = false;
                return null;
            }

            // Verificar expiración
            var expirationStr = await SecureStorage.GetAsync(Constants.TokenExpirationKey);
            if (!string.IsNullOrEmpty(expirationStr) && DateTime.TryParse(expirationStr, out var expiration))
            {
                if (expiration <= DateTime.UtcNow)
                {
                    // Token expirado, intentar refrescar
                    var refreshed = await RefreshTokenAsync();
                    if (!refreshed)
                    {
                        await LogoutAsync();
                        return null;
                    }
                    token = await SecureStorage.GetAsync(Constants.AuthTokenKey);
                }
            }

            _isAuthenticated = true;
            return token;
        }
        catch
        {
            _isAuthenticated = false;
            return null;
        }
    }

    public async Task<UserInfo?> GetCurrentUserAsync()
    {
        if (_currentUser != null)
            return _currentUser;

        try
        {
            var userId = await SecureStorage.GetAsync(Constants.UserIdKey);
            var email = await SecureStorage.GetAsync(Constants.UserEmailKey);
            var userName = await SecureStorage.GetAsync(Constants.UserNameKey);
            var rolesJson = await SecureStorage.GetAsync(Constants.UserRolesKey);

            if (string.IsNullOrEmpty(userId))
                return null;

            var roles = new List<string>();
            if (!string.IsNullOrEmpty(rolesJson))
            {
                roles = JsonSerializer.Deserialize<List<string>>(rolesJson, _jsonOptions) ?? new List<string>();
            }

            _currentUser = new UserInfo
            {
                Id = userId,
                Email = email ?? string.Empty,
                UserName = userName ?? string.Empty,
                Roles = roles
            };

            return _currentUser;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> IsInRoleAsync(string role)
    {
        var user = await GetCurrentUserAsync();
        return user?.Roles.Contains(role, StringComparer.OrdinalIgnoreCase) ?? false;
    }

    public async Task<bool> IsInAnyRoleAsync(params string[] roles)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return false;
        
        return roles.Any(r => user.Roles.Contains(r, StringComparer.OrdinalIgnoreCase));
    }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(string email, string password)
    {
        try
        {
            var request = new LoginRequest { Email = email, Password = password };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/login", request);
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, _jsonOptions);

            if (result?.Success == true && result.Data != null)
            {
                await SaveAuthDataAsync(result.Data);
                _isAuthenticated = true;
            }

            return result ?? new ApiResponse<LoginResponse> 
            { 
                Success = false, 
                Message = Constants.ErrorGenerico 
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = Constants.ErrorConexion
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            // Limpiar SecureStorage
            SecureStorage.Remove(Constants.AuthTokenKey);
            SecureStorage.Remove(Constants.RefreshTokenKey);
            SecureStorage.Remove(Constants.UserIdKey);
            SecureStorage.Remove(Constants.UserEmailKey);
            SecureStorage.Remove(Constants.UserNameKey);
            SecureStorage.Remove(Constants.UserRolesKey);
            SecureStorage.Remove(Constants.TokenExpirationKey);
        }
        catch
        {
            // Ignorar errores al limpiar
        }
        
        _currentUser = null;
        _isAuthenticated = false;
        
        await Task.CompletedTask;
    }

    public async Task<ApiResponse<LoginResponse>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync("/api/auth/register", request);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, _jsonOptions);

            if (result?.Success == true && result.Data != null)
            {
                await SaveAuthDataAsync(result.Data);
                _isAuthenticated = true;
            }

            return result ?? new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = Constants.ErrorGenerico
            };
        }
        catch (HttpRequestException)
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = Constants.ErrorConexion
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<LoginResponse>
            {
                Success = false,
                Message = ex.Message
            };
        }
    }

    public async Task<bool> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await SecureStorage.GetAsync(Constants.RefreshTokenKey);
            var token = await SecureStorage.GetAsync(Constants.AuthTokenKey);
            
            if (string.IsNullOrEmpty(refreshToken) || string.IsNullOrEmpty(token))
                return false;

            var request = new { Token = token, RefreshToken = refreshToken };
            var response = await _httpClient.PostAsJsonAsync("/api/auth/refresh", request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var result = JsonSerializer.Deserialize<ApiResponse<LoginResponse>>(content, _jsonOptions);
                
                if (result?.Success == true && result.Data != null)
                {
                    await SaveAuthDataAsync(result.Data);
                    return true;
                }
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    private async Task SaveAuthDataAsync(LoginResponse data)
    {
        await SecureStorage.SetAsync(Constants.AuthTokenKey, data.Token);
        await SecureStorage.SetAsync(Constants.RefreshTokenKey, data.RefreshToken);
        await SecureStorage.SetAsync(Constants.TokenExpirationKey, data.Expiration.ToString("O"));
        await SecureStorage.SetAsync(Constants.UserIdKey, data.User.Id);
        await SecureStorage.SetAsync(Constants.UserEmailKey, data.User.Email);
        await SecureStorage.SetAsync(Constants.UserNameKey, data.User.UserName);
        await SecureStorage.SetAsync(Constants.UserRolesKey, JsonSerializer.Serialize(data.User.Roles));
        
        _currentUser = data.User;
    }
}
