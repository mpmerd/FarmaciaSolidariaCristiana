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
