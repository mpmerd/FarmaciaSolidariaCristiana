using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// Controlador de diagnóstico para verificar que la API funciona correctamente.
    /// Este controlador NO hereda de ApiBaseController para aislar problemas.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosticsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IServiceProvider _serviceProvider;

        public DiagnosticsController(
            IConfiguration configuration,
            IServiceProvider serviceProvider)
        {
            _configuration = configuration;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Ping simple - retorna OK si el servidor está funcionando
        /// </summary>
        [HttpGet("ping")]
        [AllowAnonymous]
        public IActionResult Ping()
        {
            return Ok(new { 
                status = "OK", 
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown"
            });
        }

        /// <summary>
        /// Verifica la configuración básica
        /// </summary>
        [HttpGet("config")]
        [AllowAnonymous]
        public IActionResult CheckConfig()
        {
            try
            {
                var hasConnectionString = !string.IsNullOrEmpty(_configuration.GetConnectionString("DefaultConnection"));
                var hasJwtSecret = !string.IsNullOrEmpty(_configuration["JwtSettings:SecretKey"]);
                var hasOneSignalAppId = !string.IsNullOrEmpty(_configuration["OneSignalSettings:AppId"]);
                var oneSignalConfigured = hasOneSignalAppId && 
                    !_configuration["OneSignalSettings:AppId"]!.StartsWith("TU_");

                return Ok(new
                {
                    status = "OK",
                    hasConnectionString,
                    hasJwtSecret,
                    hasOneSignalAppId,
                    oneSignalConfigured,
                    jwtIssuer = _configuration["JwtSettings:Issuer"],
                    jwtAudience = _configuration["JwtSettings:Audience"]
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Verifica los servicios registrados
        /// </summary>
        [HttpGet("services")]
        [AllowAnonymous]
        public IActionResult CheckServices()
        {
            var results = new Dictionary<string, string>();
            
            try
            {
                // Verificar DbContext
                using var scope = _serviceProvider.CreateScope();
                
                try
                {
                    var dbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
                    results["ApplicationDbContext"] = dbContext != null ? "OK" : "NULL";
                }
                catch (Exception ex)
                {
                    results["ApplicationDbContext"] = $"ERROR: {ex.Message}";
                }

                try
                {
                    var notificationService = scope.ServiceProvider.GetService<IOneSignalNotificationService>();
                    results["IOneSignalNotificationService"] = notificationService != null 
                        ? $"OK ({notificationService.GetType().Name})" 
                        : "NULL";
                }
                catch (Exception ex)
                {
                    results["IOneSignalNotificationService"] = $"ERROR: {ex.Message}";
                }

                return Ok(new
                {
                    status = "OK",
                    services = results
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Verifica la conexión a la base de datos
        /// </summary>
        [HttpGet("database")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckDatabase()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                var canConnect = await dbContext.Database.CanConnectAsync();
                
                return Ok(new
                {
                    status = canConnect ? "OK" : "ERROR",
                    canConnect,
                    provider = dbContext.Database.ProviderName
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Endpoint que requiere autenticación para diagnosticar problemas de auth
        /// </summary>
        [HttpGet("jwt-test")]
        [Authorize]
        public IActionResult TestJwtAuth()
        {
            return Ok(new
            {
                status = "OK",
                message = "JWT authentication working",
                user = User.Identity?.Name,
                isAuthenticated = User.Identity?.IsAuthenticated ?? false
            });
        }

        /// <summary>
        /// Endpoint que hereda el comportamiento de ApiBaseController para diagnosticar
        /// </summary>
        [HttpGet("auth-error-details")]
        [AllowAnonymous]
        public IActionResult GetAuthErrorDetails()
        {
            try
            {
                // Verificar si la configuración JWT está completa
                var secretKey = _configuration["JwtSettings:SecretKey"];
                var issuer = _configuration["JwtSettings:Issuer"];
                var audience = _configuration["JwtSettings:Audience"];
                
                var errors = new List<string>();
                
                if (string.IsNullOrEmpty(secretKey))
                    errors.Add("JwtSettings:SecretKey is missing");
                else if (secretKey.Length < 32)
                    errors.Add($"JwtSettings:SecretKey is too short ({secretKey.Length} chars, need 32+)");
                    
                if (string.IsNullOrEmpty(issuer))
                    errors.Add("JwtSettings:Issuer is missing");
                    
                if (string.IsNullOrEmpty(audience))
                    errors.Add("JwtSettings:Audience is missing");
                
                return Ok(new
                {
                    status = errors.Count == 0 ? "OK" : "ERRORS",
                    errors,
                    secretKeyLength = secretKey?.Length ?? 0,
                    issuer,
                    audience,
                    hint = "If auth is failing, check that the secret key is at least 32 characters"
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Probar si UserManager funciona
        /// </summary>
        [HttpGet("test-identity")]
        [AllowAnonymous]
        public async Task<IActionResult> TestIdentity()
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
                
                // Intentar obtener cualquier usuario
                var users = userManager.Users.Take(1).ToList();
                
                return Ok(new
                {
                    status = "OK",
                    message = "UserManager working",
                    userCount = users.Count,
                    hasUsers = users.Any()
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Probar login simulado
        /// </summary>
        [HttpPost("test-login")]
        [AllowAnonymous]
        public async Task<IActionResult> TestLogin([FromBody] TestLoginDto model)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
                var signInManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.SignInManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
                
                // Buscar usuario
                var user = await userManager.FindByEmailAsync(model.Email ?? "");
                if (user == null)
                {
                    return Ok(new { status = "USER_NOT_FOUND", email = model.Email });
                }

                // Verificar password
                var result = await signInManager.CheckPasswordSignInAsync(user, model.Password ?? "", lockoutOnFailure: false);
                
                return Ok(new
                {
                    status = result.Succeeded ? "OK" : "FAILED",
                    userId = user.Id,
                    succeeded = result.Succeeded,
                    isLockedOut = result.IsLockedOut,
                    isNotAllowed = result.IsNotAllowed
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))
                });
            }
        }

        /// <summary>
        /// Verificar ensamblados JWT cargados
        /// </summary>
        [HttpGet("check-assemblies")]
        [AllowAnonymous]
        public IActionResult CheckAssemblies()
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Where(a => a.FullName != null && 
                        (a.FullName.Contains("IdentityModel") || 
                         a.FullName.Contains("Jwt") ||
                         a.FullName.Contains("Identity")))
                    .Select(a => new { 
                        Name = a.GetName().Name, 
                        Version = a.GetName().Version?.ToString(),
                        Location = a.Location
                    })
                    .ToList();
                
                return Ok(new
                {
                    status = "OK",
                    count = assemblies.Count,
                    assemblies
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Probar solo la creación del handler
        /// </summary>
        [HttpGet("test-jwt-handler")]
        [AllowAnonymous]
        public IActionResult TestJwtHandler()
        {
            try
            {
                var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                return Ok(new
                {
                    status = "OK",
                    handlerType = handler.GetType().FullName,
                    canReadToken = handler.CanReadToken("test")
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    error = ex.Message,
                    exceptionType = ex.GetType().FullName,
                    stackTrace = ex.StackTrace?.Substring(0, Math.Min(500, ex.StackTrace?.Length ?? 0))
                });
            }
        }

        /// <summary>
        /// Probar solo SymmetricSecurityKey
        /// </summary>
        [HttpGet("test-symmetric-key")]
        [AllowAnonymous]
        public IActionResult TestSymmetricKey()
        {
            try
            {
                var keyBytes = System.Text.Encoding.UTF8.GetBytes("test-key-12345678901234567890123456");
                var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
                return Ok(new
                {
                    status = "OK",
                    keySize = key.KeySize
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    error = ex.Message,
                    exceptionType = ex.GetType().FullName
                });
            }
        }

        /// <summary>
        /// Probar generación de token JWT - paso a paso
        /// </summary>
        [HttpGet("test-jwt-simple")]
        [AllowAnonymous]
        public IActionResult TestJwtSimple()
        {
            var steps = new List<string>();
            
            try
            {
                steps.Add("Step 1: Starting");
                
                // Obtener configuración JWT
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"];
                steps.Add($"Step 2: SecretKey length = {secretKey?.Length ?? 0}");

                if (string.IsNullOrEmpty(secretKey))
                {
                    return Ok(new { status = "NO_SECRET_KEY", steps });
                }

                var issuer = jwtSettings["Issuer"] ?? "FarmaciaSolidariaCristiana";
                var audience = jwtSettings["Audience"] ?? "FarmaciaSolidariaCristianaApi";
                steps.Add($"Step 3: Issuer={issuer}, Audience={audience}");

                // Crear key
                var keyBytes = System.Text.Encoding.UTF8.GetBytes(secretKey);
                steps.Add($"Step 4: Key bytes length = {keyBytes.Length}");

                var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
                steps.Add("Step 5: Created SymmetricSecurityKey");

                var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
                steps.Add("Step 6: Created SigningCredentials");

                // Crear claims simples
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim("test", "value")
                };
                steps.Add($"Step 7: Created {claims.Count} claims");

                var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
                {
                    Subject = new System.Security.Claims.ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(5),
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = credentials
                };
                steps.Add("Step 8: Created SecurityTokenDescriptor");

                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                steps.Add("Step 9: Created JwtSecurityTokenHandler");

                var token = tokenHandler.CreateToken(tokenDescriptor);
                steps.Add("Step 10: Created token");

                var tokenString = tokenHandler.WriteToken(token);
                steps.Add($"Step 11: Token string length = {tokenString.Length}");

                return Ok(new
                {
                    status = "OK",
                    steps,
                    tokenLength = tokenString.Length,
                    tokenPreview = tokenString.Substring(0, Math.Min(80, tokenString.Length)) + "..."
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    steps,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    exceptionType = ex.GetType().FullName
                });
            }
        }

        /// <summary>
        /// Probar generación de token JWT - paso a paso
        /// </summary>
        [HttpPost("test-jwt")]
        [AllowAnonymous]
        public async Task<IActionResult> TestJwtGeneration([FromBody] TestLoginDto model)
        {
            var steps = new List<string>();
            
            try
            {
                steps.Add("Step 1: Starting");
                
                using var scope = _serviceProvider.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<Microsoft.AspNetCore.Identity.IdentityUser>>();
                steps.Add("Step 2: Got UserManager");
                
                var user = await userManager.FindByEmailAsync(model.Email ?? "");
                if (user == null)
                {
                    return Ok(new { status = "USER_NOT_FOUND", steps });
                }
                steps.Add("Step 3: Found user");

                var roles = await userManager.GetRolesAsync(user);
                steps.Add($"Step 4: Got {roles.Count} roles");
                
                // Obtener configuración JWT
                var jwtSettings = _configuration.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"];
                steps.Add($"Step 5: SecretKey length = {secretKey?.Length ?? 0}");

                if (string.IsNullOrEmpty(secretKey))
                {
                    return Ok(new { status = "NO_SECRET_KEY", steps });
                }

                var issuer = jwtSettings["Issuer"] ?? "FarmaciaSolidariaCristiana";
                var audience = jwtSettings["Audience"] ?? "FarmaciaSolidariaCristianaApi";
                steps.Add($"Step 6: Issuer={issuer}, Audience={audience}");

                // Crear key
                var keyBytes = System.Text.Encoding.UTF8.GetBytes(secretKey);
                steps.Add($"Step 7: Key bytes length = {keyBytes.Length}");

                var key = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(keyBytes);
                steps.Add("Step 8: Created SymmetricSecurityKey");

                var credentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                    key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);
                steps.Add("Step 9: Created SigningCredentials");

                // Crear claims
                var claims = new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier, user.Id),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Email, user.Email ?? ""),
                    new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Name, user.UserName ?? "")
                };
                steps.Add($"Step 10: Created {claims.Count} claims");

                var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
                {
                    Subject = new System.Security.Claims.ClaimsIdentity(claims),
                    Expires = DateTime.UtcNow.AddMinutes(480),
                    Issuer = issuer,
                    Audience = audience,
                    SigningCredentials = credentials
                };
                steps.Add("Step 11: Created SecurityTokenDescriptor");

                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                steps.Add("Step 12: Created JwtSecurityTokenHandler");

                var token = tokenHandler.CreateToken(tokenDescriptor);
                steps.Add("Step 13: Created token");

                var tokenString = tokenHandler.WriteToken(token);
                steps.Add($"Step 14: Token string length = {tokenString.Length}");

                return Ok(new
                {
                    status = "OK",
                    steps,
                    tokenLength = tokenString.Length,
                    tokenPreview = tokenString.Substring(0, Math.Min(50, tokenString.Length)) + "..."
                });
            }
            catch (Exception ex)
            {
                return Ok(new
                {
                    status = "ERROR",
                    steps,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    exceptionType = ex.GetType().FullName
                });
            }
        }
    }

    public class TestLoginDto
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
    }
}
