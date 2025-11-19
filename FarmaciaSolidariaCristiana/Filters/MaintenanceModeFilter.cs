using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Filters
{
    /// <summary>
    /// Filtro que verifica si la aplicación está en modo de mantenimiento
    /// Los usuarios Admin pueden seguir accediendo
    /// </summary>
    public class MaintenanceModeFilter : IActionFilter
    {
        private readonly IMaintenanceService _maintenanceService;

        public MaintenanceModeFilter(IMaintenanceService maintenanceService)
        {
            _maintenanceService = maintenanceService;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            // Si no está en modo mantenimiento, continuar normalmente
            if (!_maintenanceService.IsMaintenanceMode())
                return;

            // Permitir acceso a la página de mantenimiento misma
            var controller = context.RouteData.Values["controller"]?.ToString();
            var action = context.RouteData.Values["action"]?.ToString();
            
            if (controller == "Maintenance")
                return;

            // Permitir acceso a rutas de autenticación (Login, Logout)
            // Esto permite que los Admin puedan iniciar sesión durante el mantenimiento
            if (controller == "Account" && (action == "Login" || action == "Logout"))
                return;

            // Permitir a los Admin seguir usando la aplicación
            if (context.HttpContext.User.IsInRole("Admin"))
                return;

            // Para todos los demás usuarios, redirigir a la página de mantenimiento
            context.Result = new RedirectToActionResult("Index", "Maintenance", null);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No necesitamos hacer nada después de la acción
        }
    }
}
