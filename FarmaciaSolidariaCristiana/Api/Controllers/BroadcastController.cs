using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using FarmaciaSolidariaCristiana.Data;
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
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BroadcastController> _logger;

        public BroadcastController(
            UserManager<IdentityUser> userManager,
            IEmailService emailService,
            IPendingNotificationService pendingNotificationService,
            ApplicationDbContext context,
            ILogger<BroadcastController> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _pendingNotificationService = pendingNotificationService;
            _context = context;
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
            
            // Obtener IDs de usuarios que tienen la app móvil registrada (dispositivo activo)
            var mobileUserIds = request.SendNotification
                ? await _context.UserDeviceTokens
                    .Where(d => d.IsActive)
                    .Select(d => d.UserId)
                    .Distinct()
                    .ToListAsync()
                : new List<string>();

            int notificationsCreated = 0;

            _logger.LogInformation(
                "[Broadcast] Admin iniciando broadcast: '{Title}' a {Count} usuarios ({MobileCount} con app). Email={SendEmail}, App={SendApp}",
                request.Title, users.Count, mobileUserIds.Count, request.SendEmail, request.SendNotification);

            // Canal 2: Notificaciones in-app - se crean inmediatamente (son rápidas)
            foreach (var user in users)
            {
                if (request.SendNotification && mobileUserIds.Contains(user.Id))
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

            // Canal 1: Emails - se envían en segundo plano (1 por minuto, tarda horas)
            int totalEmailTargets = 0;
            if (request.SendEmail)
            {
                var emailTargets = users
                    .Where(u => !string.IsNullOrEmpty(u.Email))
                    .Select(u => u.Email!)
                    .ToList();
                totalEmailTargets = emailTargets.Count;

                var emailTitle = request.Title;
                var emailMessage = request.Message;

                _ = Task.Run(async () =>
                {
                    int sent = 0;
                    int failed = 0;
                    foreach (var email in emailTargets)
                    {
                        try
                        {
                            if (sent > 0 || failed > 0)
                                await Task.Delay(60_000);

                            var emailBody = BuildEmailBody(emailTitle, emailMessage);
                            await _emailService.SendEmailAsync(email, $"📢 {emailTitle}", emailBody);
                            sent++;

                            if (sent % 10 == 0)
                                _logger.LogInformation("[Broadcast] Progreso emails: {Sent} enviados, {Failed} fallidos de {Total}",
                                    sent, failed, emailTargets.Count);
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            _logger.LogWarning(ex, "[Broadcast] Error enviando email a {Email}", email);
                            await Task.Delay(120_000);
                        }
                    }
                    _logger.LogInformation(
                        "[Broadcast] Emails completados: {Sent} enviados, {Failed} fallidos de {Total}",
                        sent, failed, emailTargets.Count);
                });
            }

            _logger.LogInformation(
                "[Broadcast] Respuesta inmediata: {Notifications} notificaciones creadas, {EmailTargets} emails en cola",
                notificationsCreated, totalEmailTargets);

            return ApiOk(new BroadcastResult
            {
                TotalUsers = users.Count,
                EmailsSent = totalEmailTargets,
                EmailsFailed = 0,
                NotificationsCreated = notificationsCreated
            }, $"Notificación masiva iniciada. {notificationsCreated} notificaciones creadas. {totalEmailTargets} emails enviándose en segundo plano.");
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
