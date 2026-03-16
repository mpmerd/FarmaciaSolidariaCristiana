using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// Controlador API para enviar notificaciones masivas a todos los usuarios.
    /// Solo accesible por administradores.
    /// Envía por dos canales: email + notificación in-app (polling).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},Identity.Application")]
    [Authorize(Roles = "Admin")]
    public class BroadcastController : ApiBaseController
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IPendingNotificationService _pendingNotificationService;
        private readonly ILogger<BroadcastController> _logger;

        public BroadcastController(
            UserManager<IdentityUser> userManager,
            IEmailService emailService,
            IPendingNotificationService pendingNotificationService,
            ILogger<BroadcastController> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _pendingNotificationService = pendingNotificationService;
            _logger = logger;
        }

        /// <summary>
        /// Envía una notificación masiva a todos los usuarios.
        /// Canales: email + notificación in-app (polling).
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendBroadcast([FromBody] BroadcastRequest request)
        {
            if (!ModelState.IsValid)
                return ApiError("Datos inválidos");

            var users = _userManager.Users.ToList();
            
            int emailsSent = 0;
            int emailsFailed = 0;
            int notificationsCreated = 0;

            _logger.LogInformation(
                "[Broadcast] Admin iniciando broadcast: '{Title}' a {Count} usuarios. Email={SendEmail}, App={SendApp}",
                request.Title, users.Count, request.SendEmail, request.SendNotification);

            foreach (var user in users)
            {
                // Canal 1: Email
                if (request.SendEmail && !string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        var emailBody = BuildEmailBody(request.Title, request.Message);
                        await _emailService.SendEmailAsync(user.Email, $"📢 {request.Title}", emailBody);
                        emailsSent++;
                    }
                    catch (Exception ex)
                    {
                        emailsFailed++;
                        _logger.LogWarning(ex, "[Broadcast] Error enviando email a {Email}", user.Email);
                    }
                }

                // Canal 2: Notificación in-app (polling)
                if (request.SendNotification)
                {
                    try
                    {
                        await _pendingNotificationService.CreateNotificationAsync(
                            user.Id,
                            request.Title,
                            request.Message,
                            NotificationTypes.General);
                        notificationsCreated++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Broadcast] Error creando notificación para usuario {UserId}", user.Id);
                    }
                }
            }

            _logger.LogInformation(
                "[Broadcast] Completado: {EmailsSent} emails enviados, {EmailsFailed} fallidos, {NotificationsCreated} notificaciones creadas",
                emailsSent, emailsFailed, notificationsCreated);

            return ApiOk(new BroadcastResult
            {
                TotalUsers = users.Count,
                EmailsSent = emailsSent,
                EmailsFailed = emailsFailed,
                NotificationsCreated = notificationsCreated
            }, "Notificación masiva enviada correctamente");
        }

        private static string BuildEmailBody(string title, string message)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, sans-serif; margin: 0; padding: 0; background-color: #f5f5f5;'>
    <div style='max-width: 600px; margin: 20px auto; background: white; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1);'>
        <div style='background: linear-gradient(135deg, #0d6efd, #0a58ca); padding: 30px; text-align: center;'>
            <h1 style='color: white; margin: 0; font-size: 22px;'>📢 {System.Net.WebUtility.HtmlEncode(title)}</h1>
        </div>
        <div style='padding: 30px;'>
            <p style='color: #333; font-size: 16px; line-height: 1.6; white-space: pre-wrap;'>{System.Net.WebUtility.HtmlEncode(message)}</p>
        </div>
        <div style='background: #f8f9fa; padding: 20px; text-align: center; border-top: 1px solid #e9ecef;'>
            <p style='color: #6c757d; font-size: 13px; margin: 0;'>Farmacia Solidaria Cristiana</p>
            <p style='color: #adb5bd; font-size: 11px; margin: 5px 0 0;'>Este mensaje fue enviado a todos los usuarios registrados.</p>
        </div>
    </div>
</body>
</html>";
        }
    }

    public class BroadcastRequest
    {
        [Required(ErrorMessage = "El título es requerido")]
        [StringLength(150, ErrorMessage = "El título no puede exceder 150 caracteres")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "El mensaje es requerido")]
        [StringLength(2000, ErrorMessage = "El mensaje no puede exceder 2000 caracteres")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Enviar por email
        /// </summary>
        public bool SendEmail { get; set; } = true;

        /// <summary>
        /// Enviar como notificación in-app (polling)
        /// </summary>
        public bool SendNotification { get; set; } = true;
    }

    public class BroadcastResult
    {
        public int TotalUsers { get; set; }
        public int EmailsSent { get; set; }
        public int EmailsFailed { get; set; }
        public int NotificationsCreated { get; set; }
    }
}
