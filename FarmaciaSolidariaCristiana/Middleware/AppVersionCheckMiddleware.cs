namespace FarmaciaSolidariaCristiana.Middleware;

/// <summary>
/// Middleware que verifica la versión de la app móvil en peticiones a la API.
/// Bloquea versiones anteriores a la mínima requerida.
/// Solo aplica a rutas /api/* — no afecta la web MVC.
/// </summary>
public class AppVersionCheckMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly Version MinimumAppVersion = new("1.0.5");

    public AppVersionCheckMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Solo aplicar a rutas de API, no a la web MVC
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        // Si la petición trae el header X-App-Version, verificar la versión
        var appVersionHeader = context.Request.Headers["X-App-Version"].FirstOrDefault();

        if (appVersionHeader != null)
        {
            // Tiene el header — verificar que sea >= mínimo
            if (Version.TryParse(appVersionHeader, out var appVersion) && appVersion >= MinimumAppVersion)
            {
                await _next(context);
                return;
            }

            // Versión bajo el mínimo
            context.Response.StatusCode = 426; // Upgrade Required
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                message = $"Tu versión de la app ({appVersionHeader}) ya no es compatible. " +
                          $"Descarga la versión {MinimumAppVersion} o superior desde la página de descarga."
            }));
            return;
        }

        // No tiene el header — podría ser un navegador o la app vieja (1.0.4)
        // Verificar si es una petición de la app móvil (lleva Bearer token pero no el header)
        var hasBearer = context.Request.Headers.Authorization.FirstOrDefault()?.StartsWith("Bearer ") == true;

        if (hasBearer)
        {
            // Es la app móvil vieja sin el header de versión
            context.Response.StatusCode = 426;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                message = "Tu versión de la app ya no es compatible. " +
                          "Descarga la última versión desde: https://farmaciasolidaria.somee.com/android/"
            }));
            return;
        }

        // Sin Bearer y sin header — es el login u otra petición pública, dejar pasar
        await _next(context);
    }
}
