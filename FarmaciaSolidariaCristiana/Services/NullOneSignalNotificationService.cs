using FarmaciaSolidariaCristiana.Api.Models;
using FarmaciaSolidariaCristiana.Models;
using Microsoft.AspNetCore.Identity;

namespace FarmaciaSolidariaCristiana.Services
{
    /// <summary>
    /// Implementación nula del servicio de notificaciones OneSignal.
    /// Se usa cuando OneSignal no está configurado para evitar errores de DI.
    /// Aún así, crea notificaciones pendientes para el sistema de polling.
    /// </summary>
    public class NullOneSignalNotificationService : IOneSignalNotificationService
    {
        private readonly ILogger<NullOneSignalNotificationService> _logger;
        private readonly IPendingNotificationService _pendingNotificationService;
        private readonly UserManager<IdentityUser> _userManager;

        public NullOneSignalNotificationService(
            ILogger<NullOneSignalNotificationService> logger,
            IPendingNotificationService pendingNotificationService,
            UserManager<IdentityUser> userManager)
        {
            _logger = logger;
            _pendingNotificationService = pendingNotificationService;
            _userManager = userManager;
            _logger.LogWarning("OneSignal no está configurado. Usando implementación nula. " +
                "Las notificaciones push no estarán disponibles, pero las notificaciones por polling sí funcionan.");
        }

        private NotificationResultDto NotConfiguredResult() => new()
        {
            Success = false,
            RecipientsCount = 0,
            ErrorMessage = "OneSignal no está configurado. Configure AppId y RestApiKey en appsettings.json"
        };

        public Task<DeviceTokenResponseDto?> RegisterDeviceTokenAsync(string userId, RegisterDeviceTokenDto dto)
        {
            _logger.LogWarning("Intento de registrar dispositivo pero OneSignal no está configurado");
            return Task.FromResult<DeviceTokenResponseDto?>(null);
        }

        public Task<bool> UnregisterDeviceTokenAsync(string userId, string oneSignalPlayerId)
        {
            return Task.FromResult(false);
        }

        public Task<List<DeviceTokenResponseDto>> GetUserDeviceTokensAsync(string userId)
        {
            return Task.FromResult(new List<DeviceTokenResponseDto>());
        }

        public Task<List<DeviceTokenResponseDto>> GetAllDeviceTokensAsync()
        {
            return Task.FromResult(new List<DeviceTokenResponseDto>());
        }

        public Task<bool> UserHasPushEnabledAsync(string userId)
        {
            return Task.FromResult(false);
        }

        public Task UpdateDeviceLastActivityAsync(string userId, string deviceType)
        {
            return Task.CompletedTask;
        }

        public Task<bool> IsUserActiveOnMobileAsync(string userId)
        {
            return Task.FromResult(false);
        }

        public Task<NotificationResultDto> SendNotificationToUserAsync(
            string userId, string title, string message,
            NotificationType type = NotificationType.General,
            Dictionary<string, string>? data = null)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendNotificationToPlayersAsync(
            List<string> playerIds, string title, string message,
            NotificationType type = NotificationType.General,
            Dictionary<string, string>? data = null)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendNotificationToAllAsync(
            string title, string message,
            NotificationType type = NotificationType.General,
            Dictionary<string, string>? data = null)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendTurnoSolicitadoNotificationAsync(
            string userId, int turnoId, int numeroTurno)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendTurnoAprobadoNotificationAsync(
            string userId, int turnoId, int numeroTurno, DateTime fechaTurno, string? pdfUrl = null)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendTurnoRechazadoNotificationAsync(
            string userId, int turnoId, int numeroTurno, string motivo)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendTurnoPdfDisponibleNotificationAsync(
            string userId, int turnoId, int numeroTurno, string pdfUrl)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendTurnoRecordatorioNotificationAsync(
            string userId, int turnoId, int numeroTurno, DateTime fechaTurno)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendTurnoCanceladoNotificationAsync(
            string userId, int turnoId, int numeroTurno, string motivo)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendTurnoReprogramadoNotificationAsync(
            string userId, int turnoId, int numeroTurno, DateTime fechaOriginal, DateTime fechaNueva, string motivo)
        {
            return Task.FromResult(NotConfiguredResult());
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

                var title = "🆕 Nueva Solicitud de Turno";
                var message = $"{nombreSolicitante} ha solicitado el turno #{numeroTurno}";
                
                var data = new Dictionary<string, string>
                {
                    { "turnoId", turnoId.ToString() },
                    { "numeroTurno", numeroTurno.ToString() },
                    { "action", "revisar_turno" }
                };

                // Crear notificaciones pendientes para el sistema de polling
                var pendingNotificationsCreated = 0;
                foreach (var userId in allUserIds)
                {
                    try
                    {
                        await _pendingNotificationService.CreateNotificationAsync(
                            userId,
                            title,
                            message,
                            nameof(NotificationType.TurnoSolicitado),
                            turnoId,
                            "Turno",
                            data);
                        pendingNotificationsCreated++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error creando notificación pendiente para usuario {UserId}", userId);
                    }
                }
                
                _logger.LogInformation(
                    "Creadas {Count} notificaciones pendientes para farmacéuticos/admins sobre turno #{TurnoId} (sin push - OneSignal no configurado)",
                    pendingNotificationsCreated, turnoId);

                return new NotificationResultDto
                {
                    Success = true,
                    RecipientsCount = pendingNotificationsCreated,
                    ErrorMessage = "Push no disponible (OneSignal no configurado). Notificaciones por polling creadas."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando notificaciones pendientes para farmacéuticos");
                return NotConfiguredResult();
            }
        }

        public Task<NotificationResultDto> SendTurnoCanceladoPorPacienteToFarmaceuticosAsync(
            int turnoId, int numeroTurno, string nombrePaciente, DateTime fechaTurno, string motivo)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendTurnoCanceladoNoPresentacionAsync(
            string userId, int turnoId, int numeroTurno, DateTime fechaTurno)
        {
            return Task.FromResult(NotConfiguredResult());
        }

        public Task<NotificationResultDto> SendTurnoCanceladoNoPresentacionToFarmaceuticosAsync(
            int turnoId, int numeroTurno, string nombrePaciente, DateTime fechaTurno)
        {
            return Task.FromResult(NotConfiguredResult());
        }
    }
}
