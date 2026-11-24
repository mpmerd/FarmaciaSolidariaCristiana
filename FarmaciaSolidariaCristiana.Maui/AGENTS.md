# App MAUI — FarmaciaSolidariaCristiana.Maui (.NET MAUI, Android)

App Android (compila también iOS en macOS) que consume la API REST del backend. MVVM con CommunityToolkit.Mvvm y navegación Shell (Flyout con menús dinámicos según rol). Contexto general del repo: `../AGENTS.md`. Esquema de datos: `../docs/BASE_DE_DATOS.md`.

## Reglas al tocar código

- Toda llamada HTTP va por `IApiService` (`Services/ApiService.cs`) — nunca instanciar `HttpClient` directo. El servicio agrega `X-App-Version` y Bearer, y maneja 401→LoginPage y 426→aviso de actualización.
- URL base: `Helpers/Constants.cs` (`ApiBaseUrl`, `OneSignalAppId`) — nunca hardcodear URLs.
- ViewModels heredan de `BaseViewModel` (IsBusy, Title); comandos `[RelayCommand]` sobre métodos `async Task`; propiedades `[ObservableProperty]`; listas enlazadas a UI con `ObservableCollection<T>`.
- Lógica en el ViewModel, no en el code-behind (solo ciclo de vida, event handlers y navegación).
- Timestamps: usar `Helpers/CubaTime.cs`, nunca `DateTime.Now`/`.Today` (guard del repo).
- Colores vía `StaticResource`/`AppThemeBinding` (Resources/Styles, soporte light/dark); nunca hardcodear en XAML. Fuente OpenSans. Botones: CornerRadius 8, MinimumHeightRequest 44.
- Alertas al usuario: `await DisplayAlert(...)` con mensajes en español; capturar excepciones en los comandos.

## Servicios clave (`Services/`)

- `AuthService` — sesión JWT en `SecureStorage` (token, userId, email, roles, expiración); persiste entre reinicios.
- `PollingNotificationService` — polling cada 30 s de `/api/notifications/pending` + heartbeat; emite `NotificationReceived` y reproduce sonido.
- `NotificationsHubClient` — SignalR; fallback automático a polling/SSE si no hay WebSocket.
- `UpdateService` — al arrancar consulta `/api/maintenanceapi/status` y `version.json` del servidor (auto-actualización sin Google Play).
- `CacheService` — caché en memoria con TTL (default 5 min).
- `ImageCompressionService` — redimensiona y comprime imágenes antes de subirlas.

## Estructura

- `Views/` (páginas XAML, carpeta `Avanzado/`, popup `TextInputPopup`), `ViewModels/` (21), `Models/` (espejos de los DTOs del servidor en `AuthModels.cs`, `EntityModels.cs`, `Turno.cs`), `Converters/`, `Messages/`, `Resources/`, `Platforms/`.
- Flujo de arranque (`App.xaml.cs`): mantenimiento → actualización → ¿token válido en SecureStorage? → DashboardPage o LoginPage; luego arranca el polling de notificaciones.

## Compilar

```bash
dotnet build -f net10.0-android
dotnet publish -f net10.0-android -c Release   # APK
```

Ver `README.md` de esta carpeta para configuración (iconos, OneSignal, troubleshooting de workloads y SSL).
