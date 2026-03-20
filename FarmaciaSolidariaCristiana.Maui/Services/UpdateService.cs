using System.Text.Json;
using FarmaciaSolidariaCristiana.Maui.Helpers;

namespace FarmaciaSolidariaCristiana.Maui.Services;

public class MaintenanceStatus
{
    public bool isInMaintenance { get; set; }
    public string reason { get; set; } = string.Empty;
}

public class MaintenanceApiResponse
{
    public bool success { get; set; }
    public MaintenanceStatus? data { get; set; }
}

public class VersionInfo
{
    public string version { get; set; } = string.Empty;
    public int versionCode { get; set; }
    public string releaseDate { get; set; } = string.Empty;
    public string downloadUrl { get; set; } = string.Empty;
    public string fileSize { get; set; } = string.Empty;
    public string minAndroidVersion { get; set; } = string.Empty;
    public string minimumVersion { get; set; } = string.Empty;
    public string releaseNotes { get; set; } = string.Empty;
}

public class UpdateService
{
    private const string VERSION_URL = "https://farmaciasolidaria.somee.com/android/version.json";
    private static readonly string MAINTENANCE_URL = $"{Constants.ApiBaseUrl}/api/maintenanceapi/status";
    private readonly HttpClient _httpClient;

    public UpdateService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    /// <summary>
    /// Verifica si el servidor está en modo mantenimiento.
    /// Retorna el motivo si está activo, o null si no lo está.
    /// </summary>
    public async Task<MaintenanceStatus?> CheckMaintenanceAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(MAINTENANCE_URL);
            var result = JsonSerializer.Deserialize<MaintenanceApiResponse>(response);
            
            if (result?.success == true && result.data?.isInMaintenance == true)
            {
                return result.data;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Maintenance] Error checking maintenance status: {ex.Message}");
        }

        return null;
    }

    public async Task CheckForUpdatesAsync()
    {
        try
        {
            var serverVersion = await GetServerVersionAsync();
            if (serverVersion == null)
                return;

            var currentVersion = AppInfo.VersionString;

            // Verificar si la versión actual está por debajo de la mínima obligatoria
            if (!string.IsNullOrEmpty(serverVersion.minimumVersion) &&
                IsNewerVersion(serverVersion.minimumVersion, currentVersion))
            {
                await ForceUpdateAsync(serverVersion);
                return;
            }

            // Verificar si hay una actualización opcional disponible
            if (IsNewerVersion(serverVersion.version, currentVersion))
            {
                await PromptUserToUpdateAsync(serverVersion);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error checking for updates: {ex.Message}");
        }
    }

    private async Task<VersionInfo?> GetServerVersionAsync()
    {
        try
        {
            var response = await _httpClient.GetStringAsync(VERSION_URL);
            return JsonSerializer.Deserialize<VersionInfo>(response);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching version info: {ex.Message}");
            return null;
        }
    }

    private bool IsNewerVersion(string serverVersion, string currentVersion)
    {
        try
        {
            var server = new Version(serverVersion);
            var current = new Version(currentVersion);
            return server > current;
        }
        catch
        {
            return false;
        }
    }

    private async Task ForceUpdateAsync(VersionInfo versionInfo)
    {
        var message = $"Esta versión de la aplicación ya no es compatible y no puede ejecutarse.\n\n" +
                     $"Versión mínima requerida: {versionInfo.minimumVersion}\n" +
                     $"Tu versión actual: {AppInfo.VersionString}\n\n" +
                     $"Debes instalar la última versión para continuar.";

        // Bucle infinito: si el usuario cierra el diálogo (botón atrás, etc.)
        // se vuelve a mostrar inmediatamente. No hay forma de evadir la actualización.
        while (true)
        {
            if (Application.Current?.Windows.FirstOrDefault()?.Page == null)
            {
                await Task.Delay(500);
                continue;
            }

            await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
                "⛔ Actualización Obligatoria",
                message,
                "Descargar Actualización"
            );

            // Abrir descarga y cerrar la app
            await Launcher.OpenAsync(new Uri(versionInfo.downloadUrl));
            Application.Current.Quit();

            // Si Quit() no cerró inmediatamente, esperar y volver a mostrar
            await Task.Delay(1000);
        }
    }

    private async Task PromptUserToUpdateAsync(VersionInfo versionInfo)
    {
        var message = $"Nueva versión disponible: {versionInfo.version}\n\n" +
                     $"📅 Fecha: {versionInfo.releaseDate}\n" +
                     $"📦 Tamaño: {versionInfo.fileSize}\n\n" +
                     $"Novedades:\n{versionInfo.releaseNotes}\n\n" +
                     $"¿Desea descargar la actualización?";

        if (Application.Current?.Windows.FirstOrDefault()?.Page == null)
            return;

        bool answer = await Application.Current!.Windows[0].Page!.DisplayAlertAsync(
            "Actualización Disponible",
            message,
            "Descargar",
            "Más tarde"
        );

        if (answer)
        {
            await Launcher.OpenAsync(new Uri(versionInfo.downloadUrl));
        }
    }
}
