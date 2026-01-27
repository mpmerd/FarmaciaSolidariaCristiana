using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using FarmaciaSolidariaCristiana.Api.Models;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// Controlador de autenticación para la API.
    /// Maneja login, refresh tokens y validación de usuarios.
    /// </summary>
    public class AuthController : ApiBaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Inicia sesión y retorna un token JWT
        /// </summary>
        /// <param name="model">Credenciales de login</param>
        /// <returns>Token JWT y datos del usuario</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<LoginResponseDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos de login inválidos");
            }

            // Buscar por email o por username
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                user = await _userManager.FindByNameAsync(model.Email);
            }
            
            if (user == null)
            {
                _logger.LogWarning("Intento de login fallido para: {Email}", model.Email);
                return ApiError("Credenciales inválidas", 401);
            }

            // Verificar si el usuario está bloqueado
            if (await _userManager.IsLockedOutAsync(user))
            {
                _logger.LogWarning("Usuario bloqueado intentó iniciar sesión: {Email}", model.Email);
                return ApiError("La cuenta está bloqueada temporalmente. Intente más tarde.", 401);
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                {
                    return ApiError("La cuenta ha sido bloqueada por demasiados intentos fallidos.", 401);
                }
                
                _logger.LogWarning("Contraseña incorrecta para usuario: {Email}", model.Email);
                return ApiError("Credenciales inválidas", 401);
            }

            // Obtener roles del usuario
            var roles = await _userManager.GetRolesAsync(user);
            
            // Generar token JWT
            var token = GenerateJwtToken(user, roles);
            var refreshToken = GenerateRefreshToken();

            _logger.LogInformation("Login exitoso para usuario: {Email}", model.Email);

            return ApiOk(new LoginResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                Expiration = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                User = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email!,
                    UserName = user.UserName!,
                    Roles = roles.ToList()
                }
            }, "Login exitoso");
        }

        /// <summary>
        /// Obtiene información del usuario actual autenticado
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(typeof(ApiResponse<UserDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 401)]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return ApiError("Usuario no autenticado", 401);
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return ApiError("Usuario no encontrado", 404);
            }

            var roles = await _userManager.GetRolesAsync(user);

            return ApiOk(new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                UserName = user.UserName!,
                Roles = roles.ToList()
            });
        }

        /// <summary>
        /// Valida si el token actual es válido
        /// </summary>
        [HttpGet("validate")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        public IActionResult ValidateToken()
        {
            // Si llegamos aquí, el token es válido (el middleware ya lo validó)
            return ApiOk(true, "Token válido");
        }

        /// <summary>
        /// Cambia la contraseña del usuario actual
        /// </summary>
        [HttpPost("change-password")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId!);
            
            if (user == null)
            {
                return ApiError("Usuario no encontrado", 404);
            }

            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
            
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return ApiError($"Error al cambiar contraseña: {errors}");
            }

            _logger.LogInformation("Contraseña cambiada para usuario: {Email}", user.Email);
            return ApiOk(true, "Contraseña actualizada exitosamente");
        }

        #region Private Methods

        private string GenerateJwtToken(IdentityUser user, IList<string> roles)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no configurada");
            var issuer = jwtSettings["Issuer"] ?? "FarmaciaSolidariaCristiana";
            var audience = jwtSettings["Audience"] ?? "FarmaciaSolidariaCristianaApi";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id),
                new Claim(ClaimTypes.Email, user.Email!),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            // Agregar roles como claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            
            return tokenHandler.WriteToken(token);
        }

        private static string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = System.Security.Cryptography.RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private int GetTokenExpirationMinutes()
        {
            var expirationStr = _configuration.GetSection("JwtSettings")["ExpirationMinutes"];
            return int.TryParse(expirationStr, out var expiration) ? expiration : 480; // Default: 8 horas
        }

        #endregion
    }
}
