using FarmaciaSolidariaCristiana.Api.Models;

namespace FarmaciaSolidariaCristiana.Services
{
    /// <summary>
    /// Interfaz para el servicio de notificaciones push usando OneSignal.
    /// </summary>
    public interface IOneSignalNotificationService
    {
        // ========================================
        // Gestión de tokens de dispositivo
        // ========================================

        /// <summary>
        /// Registra un token de dispositivo OneSignal para un usuario
        /// </summary>
        Task<DeviceTokenResponseDto?> RegisterDeviceTokenAsync(string userId, RegisterDeviceTokenDto dto);

        /// <summary>
        /// Elimina un token de dispositivo de un usuario
        /// </summary>
        Task<bool> UnregisterDeviceTokenAsync(string userId, string oneSignalPlayerId);

        /// <summary>
        /// Obtiene todos los tokens activos de un usuario
        /// </summary>
        Task<List<DeviceTokenResponseDto>> GetUserDeviceTokensAsync(string userId);

        /// <summary>
        /// Verifica si un usuario tiene dispositivos registrados para push
        /// </summary>
        Task<bool> UserHasPushEnabledAsync(string userId);

        // ========================================
        // Envío de notificaciones genéricas
        // ========================================

        /// <summary>
        /// Envía una notificación push a un usuario específico (a todos sus dispositivos)
        /// </summary>
        Task<NotificationResultDto> SendNotificationToUserAsync(
            string userId,
            string title,
            string message,
            NotificationType type = NotificationType.General,
            Dictionary<string, string>? data = null);

        /// <summary>
        /// Envía una notificación push a múltiples Player IDs específicos
        /// </summary>
        Task<NotificationResultDto> SendNotificationToPlayersAsync(
            List<string> playerIds,
            string title,
            string message,
            NotificationType type = NotificationType.General,
            Dictionary<string, string>? data = null);

        /// <summary>
        /// Envía una notificación push a todos los dispositivos registrados
        /// </summary>
        Task<NotificationResultDto> SendNotificationToAllAsync(
            string title,
            string message,
            NotificationType type = NotificationType.General,
            Dictionary<string, string>? data = null);

        // ========================================
        // Notificaciones específicas de turnos
        // ========================================

        /// <summary>
        /// Notifica al usuario que su turno fue solicitado exitosamente
        /// </summary>
        Task<NotificationResultDto> SendTurnoSolicitadoNotificationAsync(
            string userId,
            int turnoId,
            int numeroTurno);

        /// <summary>
        /// Notifica al usuario que su turno fue aprobado
        /// </summary>
        Task<NotificationResultDto> SendTurnoAprobadoNotificationAsync(
            string userId,
            int turnoId,
            int numeroTurno,
            DateTime fechaTurno,
            string? pdfUrl = null);

        /// <summary>
        /// Notifica al usuario que su turno fue rechazado
        /// </summary>
        Task<NotificationResultDto> SendTurnoRechazadoNotificationAsync(
            string userId,
            int turnoId,
            int numeroTurno,
            string motivo);

        /// <summary>
        /// Notifica al usuario que el PDF de su turno está disponible
        /// </summary>
        Task<NotificationResultDto> SendTurnoPdfDisponibleNotificationAsync(
            string userId,
            int turnoId,
            int numeroTurno,
            string pdfUrl);

        /// <summary>
        /// Envía un recordatorio de turno próximo
        /// </summary>
        Task<NotificationResultDto> SendTurnoRecordatorioNotificationAsync(
            string userId,
            int turnoId,
            int numeroTurno,
            DateTime fechaTurno);

        /// <summary>
        /// Notifica al usuario que su turno fue cancelado
        /// </summary>
        Task<NotificationResultDto> SendTurnoCanceladoNotificationAsync(
            string userId,
            int turnoId,
            int numeroTurno,
            string motivo);

        /// <summary>
        /// Notifica al usuario que su turno fue reprogramado
        /// </summary>
        Task<NotificationResultDto> SendTurnoReprogramadoNotificationAsync(
            string userId,
            int turnoId,
            int numeroTurno,
            DateTime fechaOriginal,
            DateTime fechaNueva,
            string motivo);

        /// <summary>
        /// Notifica a los farmacéuticos que hay una nueva solicitud de turno
        /// </summary>
        Task<NotificationResultDto> SendNuevaSolicitudToFarmaceuticosAsync(
            int turnoId,
            int numeroTurno,
            string nombreSolicitante);
    }
}
