using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Api.Models;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Services
{
    /// <summary>
    /// Servicio para enviar notificaciones push usando la API REST de OneSignal.
    /// Documentación: https://documentation.onesignal.com/reference/create-notification
    /// </summary>
    public class OneSignalNotificationService : IOneSignalNotificationService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<OneSignalNotificationService> _logger;
        private readonly IConfiguration _configuration;

        // Configuración de OneSignal
        private readonly string? _appId;
        private readonly string? _restApiKey;
        private readonly string _apiUrl;
        private readonly bool _isConfigured;

        public OneSignalNotificationService(
            IHttpClientFactory httpClientFactory,
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<OneSignalNotificationService> logger,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
            _configuration = configuration;

            // Cargar configuración de OneSignal (tolerante a falta de configuración)
            var oneSignalSettings = _configuration.GetSection("OneSignalSettings");
            _appId = oneSignalSettings["AppId"];
            _restApiKey = oneSignalSettings["RestApiKey"];
            _apiUrl = oneSignalSettings["ApiUrl"] ?? "https://onesignal.com/api/v1";
            
            // Verificar si OneSignal está configurado correctamente
            _isConfigured = !string.IsNullOrEmpty(_appId) && 
                           !string.IsNullOrEmpty(_restApiKey) &&
                           !_appId.StartsWith("TU_") && 
                           !_restApiKey.StartsWith("TU_");
            
            if (!_isConfigured)
            {
                _logger.LogWarning("OneSignal no está configurado. Las notificaciones push estarán deshabilitadas. " +
                    "Configure AppId y RestApiKey en appsettings.json para habilitar push notifications.");
            }
            else
            {
                _logger.LogInformation("OneSignal configurado correctamente. Push notifications habilitadas.");
            }
        }

        /// <summary>
        /// Verifica si OneSignal está configurado y puede enviar notificaciones
        /// </summary>
        public bool IsConfigured => _isConfigured;

        // ========================================
        // Gestión de tokens de dispositivo
        // ========================================

        public async Task<DeviceTokenResponseDto?> RegisterDeviceTokenAsync(string userId, RegisterDeviceTokenDto dto)
        {
            try
            {
                // Verificar si el usuario existe
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("Intento de registrar token para usuario inexistente: {UserId}", userId);
                    return null;
                }

                // Buscar si ya existe este PlayerId para el usuario
                var existingToken = await _context.UserDeviceTokens
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.OneSignalPlayerId == dto.OneSignalPlayerId);

                if (existingToken != null)
                {
                    // Actualizar token existente
                    existingToken.DeviceToken = dto.DeviceToken;
                    existingToken.DeviceType = dto.DeviceType;
                    existingToken.DeviceName = dto.DeviceName;
                    existingToken.IsActive = true;
                    existingToken.UpdatedAt = DateTime.UtcNow;
                    
                    _context.UserDeviceTokens.Update(existingToken);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Token actualizado para usuario {UserId}, PlayerId: {PlayerId}", 
                        userId, dto.OneSignalPlayerId);

                    return MapToDto(existingToken);
                }

                // Verificar si este PlayerId está registrado para otro usuario (dispositivo compartido)
                var tokenForOtherUser = await _context.UserDeviceTokens
                    .FirstOrDefaultAsync(t => t.OneSignalPlayerId == dto.OneSignalPlayerId && t.UserId != userId);

                if (tokenForOtherUser != null)
                {
                    // Desactivar el token del usuario anterior
                    tokenForOtherUser.IsActive = false;
                    tokenForOtherUser.UpdatedAt = DateTime.UtcNow;
                    _context.UserDeviceTokens.Update(tokenForOtherUser);
                    
                    _logger.LogInformation("PlayerId {PlayerId} transferido del usuario {OldUserId} al usuario {NewUserId}",
                        dto.OneSignalPlayerId, tokenForOtherUser.UserId, userId);
                }

                // Crear nuevo registro de token
                var newToken = new UserDeviceToken
                {
                    UserId = userId,
                    OneSignalPlayerId = dto.OneSignalPlayerId,
                    DeviceToken = dto.DeviceToken,
                    DeviceType = dto.DeviceType,
                    DeviceName = dto.DeviceName,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.UserDeviceTokens.Add(newToken);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Nuevo token registrado para usuario {UserId}, PlayerId: {PlayerId}, Dispositivo: {DeviceType}",
                    userId, dto.OneSignalPlayerId, dto.DeviceType);

                return MapToDto(newToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar token de dispositivo para usuario {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UnregisterDeviceTokenAsync(string userId, string oneSignalPlayerId)
        {
            try
            {
                var token = await _context.UserDeviceTokens
                    .FirstOrDefaultAsync(t => t.UserId == userId && t.OneSignalPlayerId == oneSignalPlayerId);

                if (token == null)
                {
                    _logger.LogWarning("Token no encontrado para eliminar: Usuario {UserId}, PlayerId {PlayerId}",
                        userId, oneSignalPlayerId);
                    return false;
                }

                // Marcar como inactivo en lugar de eliminar (para auditoría)
                token.IsActive = false;
                token.UpdatedAt = DateTime.UtcNow;
                
                _context.UserDeviceTokens.Update(token);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Token desactivado: Usuario {UserId}, PlayerId {PlayerId}", userId, oneSignalPlayerId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al desactivar token: Usuario {UserId}, PlayerId {PlayerId}", 
                    userId, oneSignalPlayerId);
                return false;
            }
        }

        public async Task<List<DeviceTokenResponseDto>> GetUserDeviceTokensAsync(string userId)
        {
            var tokens = await _context.UserDeviceTokens
                .Where(t => t.UserId == userId && t.IsActive)
                .OrderByDescending(t => t.UpdatedAt)
                .ToListAsync();

            return tokens.Select(MapToDto).ToList();
        }

        public async Task<bool> UserHasPushEnabledAsync(string userId)
        {
            return await _context.UserDeviceTokens
                .AnyAsync(t => t.UserId == userId && t.IsActive);
        }

        // ========================================
        // Envío de notificaciones genéricas
        // ========================================

        public async Task<NotificationResultDto> SendNotificationToUserAsync(
            string userId,
            string title,
            string message,
            NotificationType type = NotificationType.General,
            Dictionary<string, string>? data = null)
        {
            // Verificar si OneSignal está configurado
            if (!_isConfigured)
            {
                _logger.LogWarning("Intento de enviar notificación pero OneSignal no está configurado");
                return new NotificationResultDto
                {
                    Success = false,
                    RecipientsCount = 0,
                    ErrorMessage = "OneSignal no está configurado. Configure AppId y RestApiKey en appsettings.json"
                };
            }

            try
            {
                // Obtener todos los Player IDs activos del usuario
                var playerIds = await _context.UserDeviceTokens
                    .Where(t => t.UserId == userId && t.IsActive)
                    .Select(t => t.OneSignalPlayerId)
                    .ToListAsync();

                if (!playerIds.Any())
                {
                    _logger.LogInformation("Usuario {UserId} no tiene dispositivos registrados para push", userId);
                    return new NotificationResultDto
                    {
                        Success = false,
                        RecipientsCount = 0,
                        ErrorMessage = "El usuario no tiene dispositivos registrados"
                    };
                }

                return await SendNotificationToPlayersAsync(playerIds, title, message, type, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación a usuario {UserId}", userId);
                return new NotificationResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<NotificationResultDto> SendNotificationToPlayersAsync(
            List<string> playerIds,
            string title,
            string message,
            NotificationType type = NotificationType.General,
            Dictionary<string, string>? data = null)
        {
            // Verificar si OneSignal está configurado
            if (!_isConfigured)
            {
                _logger.LogWarning("Intento de enviar notificación pero OneSignal no está configurado");
                return new NotificationResultDto
                {
                    Success = false,
                    RecipientsCount = 0,
                    ErrorMessage = "OneSignal no está configurado. Configure AppId y RestApiKey en appsettings.json"
                };
            }

            if (!playerIds.Any())
            {
                return new NotificationResultDto
                {
                    Success = false,
                    RecipientsCount = 0,
                    ErrorMessage = "No se especificaron destinatarios"
                };
            }

            try
            {
                var payload = BuildNotificationPayload(playerIds, title, message, type, data);
                var result = await SendToOneSignalApiAsync(payload);

                // Actualizar LastUsedAt para los tokens usados
                await UpdateTokensLastUsedAsync(playerIds);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación a {Count} dispositivos", playerIds.Count);
                return new NotificationResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public async Task<NotificationResultDto> SendNotificationToAllAsync(
            string title,
            string message,
            NotificationType type = NotificationType.General,
            Dictionary<string, string>? data = null)
        {
            // Verificar si OneSignal está configurado
            if (!_isConfigured)
            {
                _logger.LogWarning("Intento de enviar broadcast pero OneSignal no está configurado");
                return new NotificationResultDto
                {
                    Success = false,
                    RecipientsCount = 0,
                    ErrorMessage = "OneSignal no está configurado. Configure AppId y RestApiKey en appsettings.json"
                };
            }

            try
            {
                // Payload para enviar a todos los suscriptores
                var payload = new Dictionary<string, object>
                {
                    { "app_id", _appId! },
                    { "included_segments", new[] { "All" } },
                    { "headings", new { en = title, es = title } },
                    { "contents", new { en = message, es = message } },
                    { "data", BuildDataPayload(type, data) }
                };

                return await SendToOneSignalApiAsync(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación a todos los usuarios");
                return new NotificationResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ========================================
        // Notificaciones específicas de turnos
        // ========================================

        public async Task<NotificationResultDto> SendTurnoSolicitadoNotificationAsync(
            string userId, int turnoId, int numeroTurno)
        {
            var title = "Turno Solicitado";
            var message = $"Tu solicitud de turno #{numeroTurno} ha sido recibida. Te notificaremos cuando sea revisada.";
            
            var data = new Dictionary<string, string>
            {
                { "turnoId", turnoId.ToString() },
                { "numeroTurno", numeroTurno.ToString() },
                { "action", "ver_turno" }
            };

            return await SendNotificationToUserAsync(userId, title, message, NotificationType.TurnoSolicitado, data);
        }

        public async Task<NotificationResultDto> SendTurnoAprobadoNotificationAsync(
            string userId, int turnoId, int numeroTurno, DateTime fechaTurno, string? pdfUrl = null)
        {
            var title = "✅ Turno Aprobado";
            var message = $"¡Tu turno #{numeroTurno} ha sido aprobado! Fecha: {fechaTurno:dd/MM/yyyy} a las {fechaTurno:HH:mm}";
            
            var data = new Dictionary<string, string>
            {
                { "turnoId", turnoId.ToString() },
                { "numeroTurno", numeroTurno.ToString() },
                { "fechaTurno", fechaTurno.ToString("yyyy-MM-ddTHH:mm:ss") },
                { "action", "ver_turno" }
            };

            if (!string.IsNullOrEmpty(pdfUrl))
            {
                data["pdfUrl"] = pdfUrl;
                data["action"] = "descargar_pdf";
            }

            return await SendNotificationToUserAsync(userId, title, message, NotificationType.TurnoAprobado, data);
        }

        public async Task<NotificationResultDto> SendTurnoRechazadoNotificationAsync(
            string userId, int turnoId, int numeroTurno, string motivo)
        {
            var title = "❌ Turno Rechazado";
            var message = $"Tu turno #{numeroTurno} no fue aprobado. Motivo: {motivo}";
            
            var data = new Dictionary<string, string>
            {
                { "turnoId", turnoId.ToString() },
                { "numeroTurno", numeroTurno.ToString() },
                { "motivo", motivo },
                { "action", "ver_turno" }
            };

            return await SendNotificationToUserAsync(userId, title, message, NotificationType.TurnoRechazado, data);
        }

        public async Task<NotificationResultDto> SendTurnoPdfDisponibleNotificationAsync(
            string userId, int turnoId, int numeroTurno, string pdfUrl)
        {
            var title = "📄 PDF Disponible";
            var message = $"El comprobante de tu turno #{numeroTurno} está listo para descargar.";
            
            var data = new Dictionary<string, string>
            {
                { "turnoId", turnoId.ToString() },
                { "numeroTurno", numeroTurno.ToString() },
                { "pdfUrl", pdfUrl },
                { "action", "descargar_pdf" }
            };

            return await SendNotificationToUserAsync(userId, title, message, NotificationType.TurnoPdfDisponible, data);
        }

        public async Task<NotificationResultDto> SendTurnoRecordatorioNotificationAsync(
            string userId, int turnoId, int numeroTurno, DateTime fechaTurno)
        {
            var title = "⏰ Recordatorio de Turno";
            var message = $"Recuerda: Tu turno #{numeroTurno} es {fechaTurno:dddd dd/MM} a las {fechaTurno:HH:mm}";
            
            var data = new Dictionary<string, string>
            {
                { "turnoId", turnoId.ToString() },
                { "numeroTurno", numeroTurno.ToString() },
                { "fechaTurno", fechaTurno.ToString("yyyy-MM-ddTHH:mm:ss") },
                { "action", "ver_turno" }
            };

            return await SendNotificationToUserAsync(userId, title, message, NotificationType.TurnoRecordatorio, data);
        }

        public async Task<NotificationResultDto> SendTurnoCanceladoNotificationAsync(
            string userId, int turnoId, int numeroTurno, string motivo)
        {
            var title = "🚫 Turno Cancelado";
            var message = $"Tu turno #{numeroTurno} ha sido cancelado. Motivo: {motivo}";
            
            var data = new Dictionary<string, string>
            {
                { "turnoId", turnoId.ToString() },
                { "numeroTurno", numeroTurno.ToString() },
                { "motivo", motivo },
                { "action", "ver_turnos" }
            };

            return await SendNotificationToUserAsync(userId, title, message, NotificationType.TurnoCancelado, data);
        }

        public async Task<NotificationResultDto> SendTurnoReprogramadoNotificationAsync(
            string userId, int turnoId, int numeroTurno, DateTime fechaOriginal, DateTime fechaNueva, string motivo)
        {
            var title = "📅 Turno Reprogramado";
            var message = $"Tu turno #{numeroTurno} ha sido reprogramado de {fechaOriginal:dd/MM HH:mm} a {fechaNueva:dd/MM HH:mm}";
            
            var data = new Dictionary<string, string>
            {
                { "turnoId", turnoId.ToString() },
                { "numeroTurno", numeroTurno.ToString() },
                { "fechaOriginal", fechaOriginal.ToString("yyyy-MM-ddTHH:mm:ss") },
                { "fechaNueva", fechaNueva.ToString("yyyy-MM-ddTHH:mm:ss") },
                { "motivo", motivo },
                { "action", "ver_turno" }
            };

            return await SendNotificationToUserAsync(userId, title, message, NotificationType.TurnoReprogramado, data);
        }

        public async Task<NotificationResultDto> SendNuevaSolicitudToFarmaceuticosAsync(
            int turnoId, int numeroTurno, string nombreSolicitante)
        {
            try
            {
                // Obtener todos los usuarios con rol Farmaceutico o Admin
                var farmaceuticos = await _userManager.GetUsersInRoleAsync("Farmaceutico");
                var admins = await _userManager.GetUsersInRoleAsync("Admin");

                var allUserIds = farmaceuticos.Select(u => u.Id)
                    .Union(admins.Select(u => u.Id))
                    .Distinct()
                    .ToList();

                if (!allUserIds.Any())
                {
                    return new NotificationResultDto
                    {
                        Success = false,
                        RecipientsCount = 0,
                        ErrorMessage = "No hay farmacéuticos registrados"
                    };
                }

                // Obtener todos los Player IDs de farmacéuticos
                var playerIds = await _context.UserDeviceTokens
                    .Where(t => allUserIds.Contains(t.UserId) && t.IsActive)
                    .Select(t => t.OneSignalPlayerId)
                    .ToListAsync();

                if (!playerIds.Any())
                {
                    _logger.LogInformation("Los farmacéuticos no tienen dispositivos registrados para push");
                    return new NotificationResultDto
                    {
                        Success = false,
                        RecipientsCount = 0,
                        ErrorMessage = "Los farmacéuticos no tienen dispositivos registrados"
                    };
                }

                var title = "🆕 Nueva Solicitud de Turno";
                var message = $"{nombreSolicitante} ha solicitado el turno #{numeroTurno}";
                
                var data = new Dictionary<string, string>
                {
                    { "turnoId", turnoId.ToString() },
                    { "numeroTurno", numeroTurno.ToString() },
                    { "action", "revisar_turno" }
                };

                return await SendNotificationToPlayersAsync(playerIds, title, message, NotificationType.TurnoSolicitado, data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar notificación a farmacéuticos");
                return new NotificationResultDto
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ========================================
        // Métodos privados auxiliares
        // ========================================

        private Dictionary<string, object> BuildNotificationPayload(
            List<string> playerIds,
            string title,
            string message,
            NotificationType type,
            Dictionary<string, string>? data)
        {
            return new Dictionary<string, object>
            {
                { "app_id", _appId! },
                { "include_player_ids", playerIds },
                { "headings", new { en = title, es = title } },
                { "contents", new { en = message, es = message } },
                { "data", BuildDataPayload(type, data) },
                // Configuración adicional
                { "ios_badgeType", "Increase" },
                { "ios_badgeCount", 1 },
                { "android_channel_id", GetAndroidChannelForType(type) },
                { "priority", 10 }, // Alta prioridad
                { "ttl", 86400 } // Time to live: 24 horas
            };
        }

        private Dictionary<string, object> BuildDataPayload(NotificationType type, Dictionary<string, string>? additionalData)
        {
            var payload = new Dictionary<string, object>
            {
                { "notificationType", type.ToString() },
                { "timestamp", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    payload[kvp.Key] = kvp.Value;
                }
            }

            return payload;
        }

        private string GetAndroidChannelForType(NotificationType type)
        {
            // Estos canales deben configurarse en la app MAUI Android
            return type switch
            {
                NotificationType.TurnoAprobado => "turnos_aprobados",
                NotificationType.TurnoRechazado => "turnos_rechazados",
                NotificationType.TurnoRecordatorio => "recordatorios",
                _ => "general"
            };
        }

        private async Task<NotificationResultDto> SendToOneSignalApiAsync(Dictionary<string, object> payload)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = 
                    new AuthenticationHeaderValue("Basic", _restApiKey);

                var jsonContent = JsonSerializer.Serialize(payload);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                _logger.LogDebug("Enviando notificación a OneSignal: {Payload}", jsonContent);

                var response = await client.PostAsync($"{_apiUrl}/notifications", httpContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var result = JsonSerializer.Deserialize<OneSignalNotificationResponse>(responseContent, 
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    _logger.LogInformation("Notificación enviada exitosamente. ID: {NotificationId}, Destinatarios: {Recipients}",
                        result?.Id, result?.Recipients);

                    return new NotificationResultDto
                    {
                        Success = true,
                        NotificationId = result?.Id,
                        RecipientsCount = result?.Recipients ?? 0
                    };
                }
                else
                {
                    _logger.LogError("Error de OneSignal API: {StatusCode} - {Response}", 
                        response.StatusCode, responseContent);

                    return new NotificationResultDto
                    {
                        Success = false,
                        ErrorMessage = $"Error de OneSignal: {response.StatusCode} - {responseContent}"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al comunicarse con OneSignal API");
                return new NotificationResultDto
                {
                    Success = false,
                    ErrorMessage = $"Error de comunicación: {ex.Message}"
                };
            }
        }

        private async Task UpdateTokensLastUsedAsync(List<string> playerIds)
        {
            try
            {
                var tokens = await _context.UserDeviceTokens
                    .Where(t => playerIds.Contains(t.OneSignalPlayerId))
                    .ToListAsync();

                foreach (var token in tokens)
                {
                    token.LastUsedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // No fallar si no podemos actualizar esto
                _logger.LogWarning(ex, "No se pudo actualizar LastUsedAt para los tokens");
            }
        }

        private static DeviceTokenResponseDto MapToDto(UserDeviceToken token)
        {
            return new DeviceTokenResponseDto
            {
                Id = token.Id,
                OneSignalPlayerId = token.OneSignalPlayerId,
                DeviceType = token.DeviceType,
                DeviceName = token.DeviceName,
                IsActive = token.IsActive,
                CreatedAt = token.CreatedAt,
                UpdatedAt = token.UpdatedAt
            };
        }
    }
}
