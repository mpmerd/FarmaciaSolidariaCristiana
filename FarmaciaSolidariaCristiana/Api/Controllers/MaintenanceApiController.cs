using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// Endpoint público para que la app móvil consulte el estado de mantenimiento.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class MaintenanceApiController : ControllerBase
    {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenanceApiController(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        /// <summary>
        /// Retorna el estado actual de mantenimiento.
        /// No requiere autenticación para que la app pueda consultarlo siempre.
        /// </summary>
        [HttpGet("status")]
        [AllowAnonymous]
        public IActionResult GetStatus()
        {
            var isActive = _maintenanceService.IsMaintenanceMode();
            var reason = isActive ? _maintenanceService.GetMaintenanceReason() : null;

            return Ok(new
            {
                success = true,
                data = new
                {
                    isInMaintenance = isActive,
                    reason = reason ?? string.Empty
                }
            });
        }
    }
}
