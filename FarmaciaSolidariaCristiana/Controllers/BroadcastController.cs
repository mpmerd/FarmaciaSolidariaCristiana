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
    /// Email vía cola persistente (BroadcastEmailProcessor); notificación in-app vía SignalR.
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class BroadcastController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IPendingNotificationService _pendingNotificationService;
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BroadcastController> _logger;

        public BroadcastController(
            UserManager<IdentityUser> userManager,
            IPendingNotificationService pendingNotificationService,
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<BroadcastController> logger)
        {
            _userManager = userManager;
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
            ViewBag.BroadcastEmailDailyMax = _configuration.GetValue<int?>("AppSettings:BroadcastEmailDailyMax") ?? 350;
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
            // notificación in-app, por lo que se excluyen del email para no duplicar el aviso
            // ni gastar cuota de Gmail en quien ya está informado.
            var recentAppUserIds = (sendEmail && sendNotification)
                ? await _context.UserDeviceTokens
                    .Where(d => d.IsActive && d.LastActivityAt != null && d.LastActivityAt >= activityCutoff)
                    .Select(d => d.UserId)
                    .Distinct()
                    .ToListAsync()
                : new List<string>();
            var recentAppSet = recentAppUserIds.ToHashSet();

            var dailyMax = _configuration.GetValue<int?>("AppSettings:BroadcastEmailDailyMax") ?? 350;
            var intervalSeconds = _configuration.GetValue<int?>("AppSettings:BroadcastEmailIntervalSeconds") ?? 60;

            // Pacientes sin app activa → email. Sin tope: la cola persistente entrega a
            // ritmo seguro (1 cada N segundos, tope diario) y tarda días si hay cientos.
            var emailTargetUsers = sendEmail
                ? users
                    .Where(u => !string.IsNullOrEmpty(u.Email) && !recentAppSet.Contains(u.Id))
                    .ToList()
                : new List<IdentityUser>();

            int notificationsCreated = 0;

            var adminUser = User.Identity?.Name ?? "Admin";
            _logger.LogInformation(
                "[Broadcast] {Admin} iniciando broadcast: '{Title}' a {Count} pacientes ({MobileCount} con app, {EmailCount} por email). Email={SendEmail}, App={SendApp}",
                adminUser, title, users.Count, mobileUserIds.Count, emailTargetUsers.Count, sendEmail, sendNotification);

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

            // Canal 1: Emails - encolados en BD (tabla BroadcastEmailQueueItems). Los envía
            // BroadcastEmailProcessor (BackgroundService) a 1 cada intervalSeconds con tope
            // diario dailyMax. La cola persistente sobrevive reciclados del hosting y no
            // tiene tope de destinatarios (Gmail: 500/día compartidos con transaccionales).
            int totalEmailTargets = emailTargetUsers.Count;
            if (sendEmail && emailTargetUsers.Count > 0)
            {
                try
                {
                    var broadcastId = Guid.NewGuid();
                    var now = DateTime.UtcNow;
                    var items = emailTargetUsers
                        .GroupBy(u => u.Id) // 1 ítem por usuario (evita duplicados)
                        .Select(g => g.First())
                        .Select(u => new BroadcastEmailQueueItem
                        {
                            BroadcastId = broadcastId,
                            UserId = u.Id,
                            Email = u.Email!,
                            Title = title,
                            Message = message,
                            Status = BroadcastEmailStatus.Pending,
                            Attempts = 0,
                            CreatedAtUtc = now
                        })
                        .ToList();

                    await _context.BroadcastEmailQueueItems.AddRangeAsync(items);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation(
                        "[Broadcast] {Count} emails encolados (broadcast {BroadcastId}). Entrega: 1 cada {Seconds}s, tope {DailyMax}/día (~{Days} días)",
                        items.Count, broadcastId, intervalSeconds, dailyMax,
                        Math.Max(1, (int)Math.Ceiling(items.Count / (double)dailyMax)));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Broadcast] Error encolando emails");
                    totalEmailTargets = 0;
                }
            }

            var summary = new List<string>();
            if (sendNotification)
                summary.Add($"{notificationsCreated} notificaciones in-app creadas");
            if (sendEmail)
                summary.Add($"{totalEmailTargets} emails en cola a pacientes sin app activa (entrega gradual: ~{dailyMax}/día)");

            TempData["SuccessMessage"] = $"¡Notificación masiva iniciada! {string.Join(". ", summary)}.";
            return RedirectToAction("Index");
        }
    }
}
