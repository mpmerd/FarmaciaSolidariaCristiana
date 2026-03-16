using System.Text.Json;

namespace FarmaciaSolidariaCristiana.Maui.Services;

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
    private readonly HttpClient _httpClient;

    public UpdateService()
    {
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
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
        if (Application.Current?.MainPage == null)
            return;

        var message = $"Esta versión de la aplicación ya no es compatible y no puede ejecutarse.\n\n" +
                     $"Versión mínima requerida: {versionInfo.minimumVersion}\n" +
                     $"Tu versión actual: {AppInfo.VersionString}\n\n" +
                     $"Debes instalar la última versión para continuar.";

        // Diálogo de un solo botón — el usuario no puede descartar sin descargar
        await Application.Current.MainPage.DisplayAlert(
            "⛔ Actualización Obligatoria",
            message,
            "Descargar Actualización"
        );

        await Launcher.OpenAsync(new Uri(versionInfo.downloadUrl));

        // Cerrar la app; al relanzarla se verificará otra vez
        Application.Current.Quit();
    }

    private async Task PromptUserToUpdateAsync(VersionInfo versionInfo)
    {
        var message = $"Nueva versión disponible: {versionInfo.version}\n\n" +
                     $"📅 Fecha: {versionInfo.releaseDate}\n" +
                     $"📦 Tamaño: {versionInfo.fileSize}\n\n" +
                     $"Novedades:\n{versionInfo.releaseNotes}\n\n" +
                     $"¿Desea descargar la actualización?";

        if (Application.Current?.MainPage == null)
            return;

        bool answer = await Application.Current.MainPage.DisplayAlert(
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
