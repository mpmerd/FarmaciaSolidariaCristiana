namespace FarmaciaSolidariaCristiana.Services
{
    /// <summary>
    /// Servicio para gestionar el modo de mantenimiento de la aplicación
    /// </summary>
    public interface IMaintenanceService
    {
        /// <summary>
        /// Verifica si el modo de mantenimiento está activo
        /// </summary>
        bool IsMaintenanceMode();

        /// <summary>
        /// Activa el modo de mantenimiento
        /// </summary>
        void EnableMaintenanceMode(string reason);

        /// <summary>
        /// Desactiva el modo de mantenimiento
        /// </summary>
        void DisableMaintenanceMode();

        /// <summary>
        /// Obtiene el motivo del mantenimiento actual
        /// </summary>
        string? GetMaintenanceReason();
    }
}
