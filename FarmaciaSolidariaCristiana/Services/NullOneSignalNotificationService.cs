using FarmaciaSolidariaCristiana.Api.Models;

namespace FarmaciaSolidariaCristiana.Services
{
    /// <summary>
    /// Implementación nula del servicio de notificaciones OneSignal.
    /// Se usa cuando OneSignal no está configurado para evitar errores de DI.
    /// Todos los métodos retornan valores vacíos o false.
    /// </summary>
    public class NullOneSignalNotificationService : IOneSignalNotificationService
    {
        private readonly ILogger<NullOneSignalNotificationService> _logger;

        public NullOneSignalNotificationService(ILogger<NullOneSignalNotificationService> logger)
        {
            _logger = logger;
            _logger.LogWarning("OneSignal no está configurado. Usando implementación nula. " +
                "Las notificaciones push no estarán disponibles.");
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

        public Task<bool> UserHasPushEnabledAsync(string userId)
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

        public Task<NotificationResultDto> SendNuevaSolicitudToFarmaceuticosAsync(
            int turnoId, int numeroTurno, string nombreSolicitante)
        {
            return Task.FromResult(NotConfiguredResult());
        }
    }
}
