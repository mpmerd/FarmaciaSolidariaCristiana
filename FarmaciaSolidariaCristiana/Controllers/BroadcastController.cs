using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Controllers
{
    /// <summary>
    /// Controlador MVC para enviar notificaciones masivas desde la web.
    /// Solo accesible por administradores.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class BroadcastController : Controller
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

        public IActionResult Index()
        {
            ViewBag.UserCount = _userManager.Users.Count();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string title, string message, bool sendEmail, bool sendNotification)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(message))
            {
                TempData["ErrorMessage"] = "El título y el mensaje son requeridos.";
                return RedirectToAction("Index");
            }

            if (!sendEmail && !sendNotification)
            {
                TempData["ErrorMessage"] = "Debe seleccionar al menos un canal de envío.";
                return RedirectToAction("Index");
            }

            var users = _userManager.Users.ToList();

            // Obtener IDs de usuarios que tienen la app móvil registrada (dispositivo activo)
            var mobileUserIds = sendNotification
                ? await _context.UserDeviceTokens
                    .Where(d => d.IsActive)
                    .Select(d => d.UserId)
                    .Distinct()
                    .ToListAsync()
                : new List<string>();

            int emailsSent = 0;
            int emailsFailed = 0;
            int notificationsCreated = 0;

            var adminUser = User.Identity?.Name ?? "Admin";
            _logger.LogInformation(
                "[Broadcast] {Admin} iniciando broadcast: '{Title}' a {Count} usuarios ({MobileCount} con app). Email={SendEmail}, App={SendApp}",
                adminUser, title, users.Count, mobileUserIds.Count, sendEmail, sendNotification);

            foreach (var user in users)
            {
                // Canal 1: Email - se envía a TODOS los usuarios
                if (sendEmail && !string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        // Pausa entre envíos para evitar rate-limiting del servidor SMTP de Somee
                        if (emailsSent > 0 || emailsFailed > 0)
                            await Task.Delay(3000);

                        var emailBody = BuildEmailBody(title, message);
                        await _emailService.SendEmailAsync(user.Email, $"📢 {title}", emailBody);
                        emailsSent++;

                        if (emailsSent % 20 == 0)
                            _logger.LogInformation("[Broadcast] Progreso emails: {Sent} enviados, {Failed} fallidos de {Total}",
                                emailsSent, emailsFailed, users.Count);
                    }
                    catch (Exception ex)
                    {
                        emailsFailed++;
                        _logger.LogWarning(ex, "[Broadcast] Error enviando email a {Email}", user.Email);

                        // Si hay muchos fallos consecutivos, aumentar pausa (probable rate-limit)
                        if (emailsFailed > 3 && emailsFailed > emailsSent)
                            await Task.Delay(8000);
                    }
                }

                // Canal 2: Notificación in-app - solo a usuarios con app móvil registrada
                if (sendNotification && mobileUserIds.Contains(user.Id))
                {
                    try
                    {
                        await _pendingNotificationService.CreateNotificationAsync(
                            user.Id,
                            title,
                            message,
                            NotificationTypes.General);
                        notificationsCreated++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "[Broadcast] Error creando notificación para {UserId}", user.Id);
                    }
                }
            }

            _logger.LogInformation(
                "[Broadcast] Completado: {EmailsSent} emails, {EmailsFailed} fallidos, {Notifications} notificaciones",
                emailsSent, emailsFailed, notificationsCreated);

            var summary = new List<string>();
            if (sendEmail)
                summary.Add($"{emailsSent} emails enviados" + (emailsFailed > 0 ? $" ({emailsFailed} fallidos)" : ""));
            if (sendNotification)
                summary.Add($"{notificationsCreated} notificaciones en app creadas");

            TempData["SuccessMessage"] = $"Notificación masiva enviada: {string.Join(", ", summary)}.";
            return RedirectToAction("Index");
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
}
