namespace FarmaciaSolidariaCristiana.Maui.Helpers;

/// <summary>
/// Constantes de la aplicación
/// </summary>
public static class Constants
{
    // URLs de la API
#if DEBUG
    // Desarrollo local - cambiar IP según tu red
    public const string ApiBaseUrl = "http://192.168.2.104:5003";
#else
    // Producción
    public const string ApiBaseUrl = "https://farmaciasolidaria.somee.com";
#endif

    // OneSignal
    public const string OneSignalAppId = "4d981851-f1a2-4112-8a08-08500e48f196";

    // SecureStorage Keys
    public const string AuthTokenKey = "auth_token";
    public const string RefreshTokenKey = "refresh_token";
    public const string UserIdKey = "user_id";
    public const string UserEmailKey = "user_email";
    public const string UserNameKey = "user_name";
    public const string UserRolesKey = "user_roles";
    public const string TokenExpirationKey = "token_expiration";

    // Roles
    public const string RoleAdmin = "Admin";
    public const string RoleFarmaceutico = "Farmaceutico";
    public const string RoleViewer = "viewer";
    public const string RoleViewerPublic = "viewerpublic";

    // Colores de la app (estilo Bootstrap)
    public const string PrimaryColor = "#0d6efd";
    public const string SecondaryColor = "#6c757d";
    public const string SuccessColor = "#198754";
    public const string DangerColor = "#dc3545";
    public const string WarningColor = "#ffc107";
    public const string InfoColor = "#0dcaf0";
    public const string LightColor = "#f8f9fa";
    public const string DarkColor = "#212529";

    // Mensajes
    public const string ErrorGenerico = "Ocurrió un error. Por favor, intente nuevamente.";
    public const string ErrorConexion = "Error de conexión. Verifique su conexión a internet.";
    public const string SesionExpirada = "Su sesión ha expirado. Por favor, inicie sesión nuevamente.";
}
