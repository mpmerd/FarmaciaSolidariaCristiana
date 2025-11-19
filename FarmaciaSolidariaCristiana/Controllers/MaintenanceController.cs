using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Controllers
{
    /// <summary>
    /// Controlador para gestionar el modo de mantenimiento
    /// </summary>
    public class MaintenanceController : Controller
    {
        private readonly IMaintenanceService _maintenanceService;
        private readonly ILogger<MaintenanceController> _logger;

        public MaintenanceController(
            IMaintenanceService maintenanceService,
            ILogger<MaintenanceController> logger)
        {
            _maintenanceService = maintenanceService;
            _logger = logger;
        }

        /// <summary>
        /// Página pública de mantenimiento (vista para usuarios)
        /// </summary>
        [AllowAnonymous]
        public IActionResult Index()
        {
            var reason = _maintenanceService.GetMaintenanceReason() 
                ?? "La farmacia está realizando trabajos de mantenimiento.";
            
            ViewBag.Reason = reason;
            return View();
        }

        /// <summary>
        /// Panel de control para administradores
        /// </summary>
        [Authorize(Roles = "Admin")]
        public IActionResult Control()
        {
            ViewBag.IsMaintenanceMode = _maintenanceService.IsMaintenanceMode();
            ViewBag.Reason = _maintenanceService.GetMaintenanceReason();
            return View();
        }

        /// <summary>
        /// Activar modo de mantenimiento
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Enable(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                TempData["ErrorMessage"] = "Debe proporcionar un motivo para el mantenimiento";
                return RedirectToAction(nameof(Control));
            }

            try
            {
                _maintenanceService.EnableMaintenanceMode(reason);
                TempData["SuccessMessage"] = "Modo de mantenimiento activado exitosamente";
                _logger.LogInformation("Modo de mantenimiento activado por {User}: {Reason}", 
                    User.Identity?.Name, reason);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activando modo de mantenimiento");
                TempData["ErrorMessage"] = "Error al activar el modo de mantenimiento";
            }

            return RedirectToAction(nameof(Control));
        }

        /// <summary>
        /// Desactivar modo de mantenimiento
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public IActionResult Disable()
        {
            try
            {
                _maintenanceService.DisableMaintenanceMode();
                TempData["SuccessMessage"] = "Modo de mantenimiento desactivado. La aplicación está funcionando normalmente.";
                _logger.LogInformation("Modo de mantenimiento desactivado por {User}", User.Identity?.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error desactivando modo de mantenimiento");
                TempData["ErrorMessage"] = "Error al desactivar el modo de mantenimiento";
            }

            return RedirectToAction(nameof(Control));
        }
    }
}
