using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FarmaciaSolidariaCristiana.Api.Models;
using FarmaciaSolidariaCristiana.Services;
using FarmaciaSolidariaCristiana.Models;

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
        private readonly IPendingNotificationService _pendingNotificationService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<NotificationsController> _logger;

        public NotificationsController(
            IOneSignalNotificationService notificationService,
            IPendingNotificationService pendingNotificationService,
            UserManager<IdentityUser> userManager,
            ILogger<NotificationsController> logger)
        {
            _notificationService = notificationService;
            _pendingNotificationService = pendingNotificationService;
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

        // ========================================
        // POLLING: Endpoints para notificaciones sin push
        // ========================================
        
        /// <summary>
        /// Obtiene las notificaciones pendientes (no leídas) del usuario.
        /// Este endpoint debe llamarse periódicamente por la app móvil para polling.
        /// </summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<PendingNotificationsResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetPendingNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return ApiError("Usuario no autenticado", 401);

            var user = await _userManager.FindByIdAsync(userId);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();
            
            _logger.LogInformation(
                "[Polling] Usuario {UserName} (ID: {UserId}, Roles: {Roles}) solicitando notificaciones pendientes",
                user?.UserName ?? "desconocido",
                userId,
                string.Join(", ", userRoles));

            var notifications = await _pendingNotificationService.GetUnreadNotificationsAsync(userId);
            
            _logger.LogInformation(
                "[Polling] Usuario {UserId} tiene {Count} notificaciones pendientes",
                userId,
                notifications.Count);
            
            return ApiOk(new PendingNotificationsResponseDto
            {
                Count = notifications.Count,
                Notifications = notifications.Select(n => new PendingNotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    NotificationType = n.NotificationType,
                    ReferenceId = n.ReferenceId,
                    ReferenceType = n.ReferenceType,
                    CreatedAt = n.CreatedAt
                }).ToList()
            });
        }

        /// <summary>
        /// Marca una notificación como leída
        /// </summary>
        [HttpPost("pending/{id}/read")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkNotificationAsRead(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return ApiError("Usuario no autenticado", 401);

            var result = await _pendingNotificationService.MarkAsReadAsync(id, userId);
            
            if (!result)
                return ApiError("Notificación no encontrada", 404);

            return ApiOk(new { marked = true });
        }

        /// <summary>
        /// Marca todas las notificaciones del usuario como leídas
        /// </summary>
        [HttpPost("pending/read-all")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> MarkAllNotificationsAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return ApiError("Usuario no autenticado", 401);

            var count = await _pendingNotificationService.MarkAllAsReadAsync(userId);
            
            return ApiOk(new { markedCount = count });
        }

        /// <summary>
        /// Obtiene el conteo de notificaciones no leídas (para badge)
        /// </summary>
        [HttpGet("pending/count")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return ApiError("Usuario no autenticado", 401);

            var count = await _pendingNotificationService.GetUnreadCountAsync(userId);
            
            return ApiOk(new { unreadCount = count });
        }

        /// <summary>
        /// Registra la actividad del dispositivo móvil (heartbeat).
        /// Esto indica que el usuario está activo en la app y no debe recibir emails.
        /// </summary>
        [HttpPost("heartbeat")]
        [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Heartbeat([FromBody] HeartbeatDto? dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return ApiError("Usuario no autenticado", 401);

            // Actualizar el timestamp de última actividad del dispositivo
            await _notificationService.UpdateDeviceLastActivityAsync(
                userId,
                dto?.DeviceType ?? "Android"
            );

            return ApiOk(new { registered = true, timestamp = DateTime.UtcNow });
        }

        // ========================================
        // DEBUG: Endpoint temporal para verificar dispositivos registrados
        // ========================================
        
        /// <summary>
        /// [DEBUG] Lista todos los dispositivos registrados (sin auth, solo para desarrollo)
        /// </summary>
        [HttpGet("debug/all-devices")]
        [AllowAnonymous]
        public async Task<IActionResult> DebugGetAllDevices()
        {
            var devices = await _notificationService.GetAllDeviceTokensAsync();
            return ApiOk(new 
            { 
                count = devices.Count,
                devices = devices.Select(d => new 
                {
                    d.Id,
                    d.OneSignalPlayerId,
                    d.DeviceType,
                    d.DeviceName,
                    d.IsActive,
                    d.CreatedAt,
                    d.UpdatedAt
                })
            });
        }
    }
}

// DTOs para polling
public class PendingNotificationsResponseDto
{
    public int Count { get; set; }
    public List<PendingNotificationDto> Notifications { get; set; } = new();
}

public class PendingNotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string NotificationType { get; set; } = string.Empty;
    public int? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class HeartbeatDto
{
    public string? PlayerId { get; set; }
    public string? DeviceType { get; set; }
    public string? DeviceName { get; set; }
}
