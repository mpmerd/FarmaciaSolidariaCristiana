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
    }
}
