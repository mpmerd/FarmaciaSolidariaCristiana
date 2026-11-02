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
    /// Canales: email (cola persistente procesada por BroadcastEmailProcessor) +
    /// notificación in-app (SignalR con catch-up).
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [Authorize(AuthenticationSchemes = $"{JwtBearerDefaults.AuthenticationScheme},Identity.Application")]
    [Authorize(Roles = "Admin")]
    public class BroadcastController : ApiBaseController
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

        /// <summary>
        /// Envía una notificación masiva a todos los usuarios.
        /// Canales: email (cola persistente) + notificación in-app (SignalR).
        /// </summary>
        [HttpPost("send")]
        public async Task<IActionResult> SendBroadcast([FromBody] BroadcastRequest request)
        {
            if (!ModelState.IsValid)
                return ApiError("Datos inválidos");

            // Destinatarios: SOLO pacientes (ViewerPublic). Se excluyen Admin, Farmaceutico y Viewer.
            var users = await _userManager.GetUsersInRoleAsync("ViewerPublic");

            var activityDays = _configuration.GetValue<int?>("AppSettings:BroadcastEmailActivityDays") ?? 7;
            var activityCutoff = DateTime.UtcNow.AddDays(-activityDays);

            // IDs de pacientes con app móvil registrada (dispositivo activo) → canal in-app
            var mobileUserIds = request.SendNotification
                ? await _context.UserDeviceTokens
                    .Where(d => d.IsActive)
                    .Select(d => d.UserId)
                    .Distinct()
                    .ToListAsync()
                : new List<string>();

            // IDs de pacientes con app REALMENTE activa (heartbeat reciente): ya reciben la
            // notificación in-app, por lo que se excluyen del email para no duplicar el aviso
            // ni gastar cuota de Gmail en quien ya está informado.
            var recentAppUserIds = (request.SendEmail && request.SendNotification)
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
            var emailTargetUsers = request.SendEmail
                ? users
                    .Where(u => !string.IsNullOrEmpty(u.Email) && !recentAppSet.Contains(u.Id))
                    .ToList()
                : new List<IdentityUser>();

            int notificationsCreated = 0;

            _logger.LogInformation(
                "[Broadcast] Admin iniciando broadcast: '{Title}' a {Count} pacientes ({MobileCount} con app, {EmailCount} por email). Email={SendEmail}, App={SendApp}",
                request.Title, users.Count, mobileUserIds.Count, emailTargetUsers.Count, request.SendEmail, request.SendNotification);

            // Canal 2: Notificaciones in-app - bulk (1 SaveChanges + fan-out SignalR en paralelo).
            // Antes era un foreach con 1 SaveChanges por usuario → timeout de Somee para N grande.
            if (request.SendNotification && mobileUserIds.Count > 0)
            {
                var mobileSet = mobileUserIds.ToHashSet();
                var targetIds = users.Where(u => mobileSet.Contains(u.Id)).Select(u => u.Id).ToList();
                try
                {
                    var created = await _pendingNotificationService.CreateBulkNotificationsAsync(
                        targetIds, request.Title, request.Message, NotificationTypes.General);
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
            if (request.SendEmail && emailTargetUsers.Count > 0)
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
                            Title = request.Title,
                            Message = request.Message,
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

            _logger.LogInformation(
                "[Broadcast] Respuesta inmediata: {Notifications} notificaciones creadas, {EmailTargets} emails encolados",
                notificationsCreated, totalEmailTargets);

            return ApiOk(new BroadcastResult
            {
                TotalUsers = users.Count,
                EmailsSent = totalEmailTargets,
                EmailsFailed = 0,
                NotificationsCreated = notificationsCreated
            }, $"Notificación masiva iniciada. {notificationsCreated} notificaciones creadas. {totalEmailTargets} emails en cola a pacientes sin app activa (entrega gradual: ~{dailyMax}/día).");
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
        /// Enviar por email (solo a pacientes sin app activa; entrega gradual vía cola persistente)
        /// </summary>
        public bool SendEmail { get; set; } = false;

        /// <summary>
        /// Enviar como notificación in-app (SignalR + catch-up)
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
