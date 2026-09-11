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
        private readonly IConfiguration _configuration;
        private readonly ILogger<BroadcastController> _logger;

        public BroadcastController(
            UserManager<IdentityUser> userManager,
            IEmailService emailService,
            IPendingNotificationService pendingNotificationService,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<BroadcastController> logger)
        {
            _userManager = userManager;
            _emailService = emailService;
            _pendingNotificationService = pendingNotificationService;
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var patients = await _userManager.GetUsersInRoleAsync("ViewerPublic");
            var activityDays = _configuration.GetValue<int?>("AppSettings:BroadcastEmailActivityDays") ?? 7;
            var activityCutoff = DateTime.UtcNow.AddDays(-activityDays);

            var activeAppIds = (await _context.UserDeviceTokens
                .Where(d => d.IsActive && d.LastActivityAt != null && d.LastActivityAt >= activityCutoff)
                .Select(d => d.UserId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            ViewBag.UserCount = patients.Count;
            ViewBag.AppActiveCount = patients.Count(u => activeAppIds.Contains(u.Id));
            ViewBag.EmailTargetCount = patients.Count(u => !string.IsNullOrEmpty(u.Email) && !activeAppIds.Contains(u.Id));
            ViewBag.BroadcastEmailMax = _configuration.GetValue<int?>("AppSettings:BroadcastEmailMax") ?? 450;
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

            // Destinatarios: SOLO pacientes (ViewerPublic). Se excluyen Admin, Farmaceutico y Viewer.
            var users = await _userManager.GetUsersInRoleAsync("ViewerPublic");

            var activityDays = _configuration.GetValue<int?>("AppSettings:BroadcastEmailActivityDays") ?? 7;
            var activityCutoff = DateTime.UtcNow.AddDays(-activityDays);

            // IDs de pacientes con app móvil registrada (dispositivo activo) → canal in-app
            var mobileUserIds = sendNotification
                ? await _context.UserDeviceTokens
                    .Where(d => d.IsActive)
                    .Select(d => d.UserId)
                    .Distinct()
                    .ToListAsync()
                : new List<string>();

            // IDs de pacientes con app REALMENTE activa (heartbeat reciente): ya reciben la
            // notificación in-app, por lo que se excluyen del email para no agotar la cuota
            // diaria de Gmail (500/día) ni duplicar el aviso.
            var recentAppUserIds = (sendEmail && sendNotification)
                ? await _context.UserDeviceTokens
                    .Where(d => d.IsActive && d.LastActivityAt != null && d.LastActivityAt >= activityCutoff)
                    .Select(d => d.UserId)
                    .Distinct()
                    .ToListAsync()
                : new List<string>();
            var recentAppSet = recentAppUserIds.ToHashSet();

            var maxEmails = _configuration.GetValue<int?>("AppSettings:BroadcastEmailMax") ?? 450;
            var emailIntervalMinutes = _configuration.GetValue<int?>("AppSettings:BroadcastEmailIntervalMinutes") ?? 5;

            var emailTargets = sendEmail
                ? users
                    .Where(u => !string.IsNullOrEmpty(u.Email) && !recentAppSet.Contains(u.Id))
                    .Select(u => u.Email!)
                    .ToList()
                : new List<string>();

            if (sendEmail && emailTargets.Count > maxEmails)
            {
                TempData["ErrorMessage"] =
                    $"El email se enviaría a {emailTargets.Count} pacientes, superando el tope seguro de {maxEmails} " +
                    "(Gmail gratuito: 500/día compartidos con códigos de registro). " +
                    "Desactive el canal email para enviar solo la notificación in-app.";
                return RedirectToAction("Index");
            }

            int notificationsCreated = 0;

            var adminUser = User.Identity?.Name ?? "Admin";
            _logger.LogInformation(
                "[Broadcast] {Admin} iniciando broadcast: '{Title}' a {Count} pacientes ({MobileCount} con app, {EmailCount} por email). Email={SendEmail}, App={SendApp}",
                adminUser, title, users.Count, mobileUserIds.Count, emailTargets.Count, sendEmail, sendNotification);

            // Canal 2: Notificaciones in-app - bulk (1 SaveChanges + fan-out SignalR en paralelo).
            // Antes era un foreach con 1 SaveChanges por usuario → timeout de Somee para N grande.
            if (sendNotification && mobileUserIds.Count > 0)
            {
                var mobileSet = mobileUserIds.ToHashSet();
                var targetIds = users.Where(u => mobileSet.Contains(u.Id)).Select(u => u.Id).ToList();
                try
                {
                    var created = await _pendingNotificationService.CreateBulkNotificationsAsync(
                        targetIds, title, message, NotificationTypes.General);
                    notificationsCreated = created.Count;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Broadcast] Error en bulk create de notificaciones in-app");
                }
            }

            // Canal 1: Emails - se envían en segundo plano (1 cada N minutos, puede tardar más de un día)
            int totalEmailTargets = emailTargets.Count;
            if (sendEmail && emailTargets.Count > 0)
            {
                var emailTitle = title;
                var emailMessage = message;

                _ = Task.Run(async () =>
                {
                    int sent = 0;
                    int failed = 0;
                    int consecutiveFailures = 0;
                    foreach (var email in emailTargets)
                    {
                        try
                        {
                            if (sent > 0 || failed > 0)
                                await Task.Delay(TimeSpan.FromMinutes(emailIntervalMinutes));

                            var emailBody = BuildEmailBody(emailTitle, emailMessage);
                            await _emailService.SendEmailAsync(email, $"📢 {emailTitle}", emailBody);
                            sent++;
                            consecutiveFailures = 0;

                            if (sent % 10 == 0)
                                _logger.LogInformation("[Broadcast] Progreso emails: {Sent} enviados, {Failed} fallidos de {Total}",
                                    sent, failed, emailTargets.Count);
                        }
                        catch (Exception ex)
                        {
                            failed++;
                            consecutiveFailures++;
                            _logger.LogWarning(ex, "[Broadcast] Error enviando email a {Email}", email);

                            if (consecutiveFailures >= 5)
                            {
                                _logger.LogError(
                                    "[Broadcast] Envío de emails abortado: {ConsecutiveFailures} fallos consecutivos. " +
                                    "Posible bloqueo de cuota Gmail. Restantes sin intentar: {Remaining}",
                                    consecutiveFailures, emailTargets.Count - sent - failed);
                                break;
                            }

                            await Task.Delay(TimeSpan.FromMinutes(emailIntervalMinutes * 2));
                        }
                    }
                    _logger.LogInformation(
                        "[Broadcast] Emails finalizados: {Sent} enviados, {Failed} fallidos de {Total}",
                        sent, failed, emailTargets.Count);
                });
            }

            var summary = new List<string>();
            if (sendNotification)
                summary.Add($"{notificationsCreated} notificaciones in-app creadas");
            if (sendEmail)
                summary.Add($"{totalEmailTargets} emails en cola a pacientes sin app activa (1 cada {emailIntervalMinutes} minutos, en segundo plano)");

            TempData["SuccessMessage"] = $"¡Notificación masiva iniciada! {string.Join(". ", summary)}.";
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
