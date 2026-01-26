using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FarmaciaSolidariaCristiana.Api.Models;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// Controlador API para gestionar notificaciones push con OneSignal.
    /// Permite registrar tokens de dispositivo y enviar notificaciones.
    /// NOTA: La autenticación se maneja vía JWT cuando se incluye un token Bearer,
    /// o vía cookies cuando se accede desde el sitio web.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    [Produces("application/json")]
    public class NotificationsController : ApiBaseController
    {
        private readonly IOneSignalNotificationService _notificationService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            IOneSignalNotificationService notificationService,
            UserManager<IdentityUser> userManager,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _userManager = userManager;
            _logger = logger;
        }

        // ========================================
        // Gestión de tokens de dispositivo
        // ========================================

        /// <summary>
        /// Registra un token de dispositivo OneSignal para el usuario autenticado.
        /// Este endpoint debe llamarse cuando la app MAUI inicia o cuando cambia el token.
        /// </summary>
        /// <param name="dto">Datos del dispositivo a registrar</param>
        /// <returns>Información del dispositivo registrado</returns>
        /// <remarks>
        /// Ejemplo de request:
        /// 
        ///     POST /api/notifications/device
        ///     {
        ///         "oneSignalPlayerId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
        ///         "deviceToken": "token_opcional_del_dispositivo",
        ///         "deviceType": "Android",
        ///         "deviceName": "Samsung Galaxy S21"
        ///     }
        /// 
        /// </remarks>
        [HttpPost("device")]
        [ProducesResponseType(typeof(ApiResponse<DeviceTokenResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceTokenDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Datos de validación inválidos",
                    Data = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiError("No se pudo identificar al usuario", 401);
            }

            var result = await _notificationService.RegisterDeviceTokenAsync(userId, dto);
            
            if (result == null)
            {
                return ApiError("No se pudo registrar el dispositivo");
            }

            _logger.LogInformation("Dispositivo registrado: Usuario {UserId}, PlayerId {PlayerId}", 
                userId, dto.OneSignalPlayerId);

            return ApiOk(result, "Dispositivo registrado exitosamente");
        }

        /// <summary>
        /// Elimina (desactiva) un token de dispositivo del usuario autenticado.
        /// Llamar este endpoint cuando el usuario cierra sesión en la app.
        /// </summary>
        /// <param name="dto">PlayerId a eliminar</param>
        [HttpPost("device/unregister")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UnregisterDevice([FromBody] UnregisterDeviceTokenDto dto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiError("No se pudo identificar al usuario", 401);
            }

            var success = await _notificationService.UnregisterDeviceTokenAsync(userId, dto.OneSignalPlayerId);
            
            if (!success)
            {
                return ApiError("No se encontró el dispositivo a eliminar");
            }

            _logger.LogInformation("Dispositivo desregistrado: Usuario {UserId}, PlayerId {PlayerId}", 
                userId, dto.OneSignalPlayerId);

            return ApiOk<object>(null!, "Dispositivo eliminado exitosamente");
        }

        /// <summary>
        /// Obtiene todos los dispositivos registrados del usuario autenticado.
        /// </summary>
        [HttpGet("devices")]
        [ProducesResponseType(typeof(ApiResponse<List<DeviceTokenResponseDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyDevices()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiError("No se pudo identificar al usuario", 401);
            }

            var devices = await _notificationService.GetUserDeviceTokensAsync(userId);
            return ApiOk(devices);
        }

        /// <summary>
        /// Verifica si el usuario actual tiene notificaciones push habilitadas.
        /// </summary>
        [HttpGet("push-status")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPushStatus()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiError("No se pudo identificar al usuario", 401);
            }

            var hasPush = await _notificationService.UserHasPushEnabledAsync(userId);
            var devices = await _notificationService.GetUserDeviceTokensAsync(userId);
            
            return ApiOk(new
            {
                PushEnabled = hasPush,
                DeviceCount = devices.Count,
                Devices = devices.Select(d => new { d.DeviceType, d.DeviceName, d.UpdatedAt })
            });
        }

        // ========================================
        // Envío de notificaciones (Solo Admin/Farmaceutico)
        // ========================================

        /// <summary>
        /// Envía una notificación push a un usuario específico.
        /// Solo accesible por administradores y farmacéuticos.
        /// </summary>
        [HttpPost("send")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<NotificationResultDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SendNotification([FromBody] SendNotificationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Datos de validación inválidos",
                    Data = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            NotificationResultDto result;

            if (!string.IsNullOrEmpty(dto.UserId))
            {
                // Enviar a un usuario específico
                result = await _notificationService.SendNotificationToUserAsync(
                    dto.UserId, dto.Title, dto.Message, dto.Type, dto.Data);
            }
            else if (dto.PlayerIds?.Any() == true)
            {
                // Enviar a Player IDs específicos
                result = await _notificationService.SendNotificationToPlayersAsync(
                    dto.PlayerIds, dto.Title, dto.Message, dto.Type, dto.Data);
            }
            else
            {
                return ApiError("Debe especificar UserId o PlayerIds");
            }

            if (!result.Success)
            {
                return ApiError(result.ErrorMessage ?? "Error al enviar notificación");
            }

            return ApiOk(result, $"Notificación enviada a {result.RecipientsCount} dispositivo(s)");
        }

        /// <summary>
        /// Envía una notificación push a todos los usuarios registrados.
        /// Solo accesible por administradores.
        /// </summary>
        [HttpPost("send/broadcast")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<NotificationResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BroadcastNotification([FromBody] SendNotificationDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    Message = "Datos de validación inválidos",
                    Data = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            var result = await _notificationService.SendNotificationToAllAsync(
                dto.Title, dto.Message, dto.Type, dto.Data);

            if (!result.Success)
            {
                return ApiError(result.ErrorMessage ?? "Error al enviar notificación broadcast");
            }

            return ApiOk(result, "Notificación broadcast enviada");
        }

        // ========================================
        // Pruebas de notificaciones
        // ========================================

        /// <summary>
        /// Envía una notificación de prueba al dispositivo del usuario autenticado.
        /// Útil para verificar que las notificaciones funcionan correctamente.
        /// </summary>
        [HttpPost("test")]
        [ProducesResponseType(typeof(ApiResponse<NotificationResultDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SendTestNotification()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiError("No se pudo identificar al usuario", 401);
            }

            var result = await _notificationService.SendNotificationToUserAsync(
                userId,
                "🧪 Prueba de Notificación",
                "¡Las notificaciones push están funcionando correctamente!",
                NotificationType.General,
                new Dictionary<string, string>
                {
                    { "test", "true" },
                    { "timestamp", DateTime.UtcNow.ToString("o") }
                });

            if (!result.Success)
            {
                return ApiError(result.ErrorMessage ?? "No tienes dispositivos registrados");
            }

            return ApiOk(result, "Notificación de prueba enviada");
        }
    }
}
