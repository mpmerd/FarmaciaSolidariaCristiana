namespace FarmaciaSolidariaCristiana.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string body);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task SendWelcomeEmailAsync(string toEmail, string userName);
        
        /// <summary>
        /// Envía email de aprobación de turno con PDF adjunto
        /// </summary>
        Task<bool> SendTurnoAprobadoEmailAsync(string toEmail, string userName, int numeroTurno, 
            DateTime fechaTurno, string pdfPath);

        /// <summary>
        /// Envía email de rechazo de turno
        /// </summary>
        Task<bool> SendTurnoRechazadoEmailAsync(string toEmail, string userName, string motivo);

        /// <summary>
        /// Envía email de confirmación de solicitud
        /// </summary>
        Task<bool> SendTurnoSolicitadoEmailAsync(string toEmail, string userName);

        /// <summary>
        /// Envía notificación a todos los farmacéuticos cuando hay una nueva solicitud de turno
        /// </summary>
        Task<bool> SendTurnoNotificationToFarmaceuticosAsync(string userName, int turnoId, string tipoSolicitud);
    }
}
