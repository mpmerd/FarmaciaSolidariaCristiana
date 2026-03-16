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

            // Permitir acceso al endpoint de estado de mantenimiento de la API
            if (controller == "MaintenanceApi")
                return;

            // Permitir acceso a rutas de autenticación (Login, Logout)
            // Esto permite que los Admin puedan iniciar sesión durante el mantenimiento
            if (controller == "Account" && (action == "Login" || action == "Logout"))
                return;

            // Determinar si es una petición de API (para retornar JSON en vez de redirect)
            var isApiRequest = context.HttpContext.Request.Path.StartsWithSegments("/api");

            // Permitir a los Admin y Farmaceuticos seguir usando la aplicación web
            // (la app móvil se bloquea completamente vía su propia verificación)
            if (!isApiRequest && (context.HttpContext.User.IsInRole("Admin") || context.HttpContext.User.IsInRole("Farmaceutico")))
                return;

            // Para peticiones de API, retornar 503 JSON
            if (isApiRequest)
            {
                var reason = _maintenanceService.GetMaintenanceReason() ?? "Sistema en mantenimiento";
                context.Result = new ObjectResult(new
                {
                    success = false,
                    message = reason,
                    isMaintenanceMode = true
                })
                {
                    StatusCode = 503
                };
                return;
            }

            // Para todos los demás usuarios web, redirigir a la página de mantenimiento
            context.Result = new RedirectToActionResult("Index", "Maintenance", null);
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No necesitamos hacer nada después de la acción
        }
    }
}
