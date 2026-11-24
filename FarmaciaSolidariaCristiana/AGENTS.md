# Backend — FarmaciaSolidariaCristiana (ASP.NET Core 10 MVC + API)

Aplicación dual: web de gestión (MVC + Razor + Bootstrap 5) y API REST (`/api/*`) con JWT para la app MAUI. Contexto general del repo: `../AGENTS.md`. Esquema de datos completo: `../docs/BASE_DE_DATOS.md`.

## Dualidad web / API

- Misma lógica de negocio en `Services/`, expuesta dos veces: `Controllers/` (MVC, cookies Identity, `TempData` + patrón PRG) y `Api/Controllers/` (JWT, `ApiResponse<T>`).
- Auth dual registrada en `Program.cs`: esquema `Identity.Application` (cookies) + `JwtBearer`. Los controladores API usan `[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]`.
- `ApiBaseController` es la clase base de la API: helpers `ApiOk` / `ApiError` / `ApiValidationError`. Toda respuesta es `ApiResponse<T>` (Success, Message, Data, Timestamp); paginación con `PagedResult<T>`; PDFs de reportes se devuelven en Base64 (`ReportResultDto`).
- `AppVersionCheckMiddleware` exige `X-App-Version` ≥ 1.0.5 en `/api/*` con Bearer; responde HTTP 426 si falta o es inferior.
- Endpoints sin JWT: solo `POST /api/auth/login` y `GET /api/maintenanceapi/status`.

## Reglas al tocar código

- Timestamps de negocio: `CubaTime` (`Helpers/CubaTime.cs`) — nunca `DateTime.Now`/`.Today` (guard del repo).
- Nueva entidad: modelo en `Models/` → `DbSet` + relaciones en `Data/ApplicationDbContext.cs` → migración (`dotnet ef migrations add ...`) → DTOs en `Api/Models/` → controlador MVC + controlador API heredando de `ApiBaseController`.
- Comentarios y mensajes de error en español; `/// <summary>` en clases/métodos públicos de servicios y controladores.
- Nomenclatura: DTOs sufijo `Dto`, ViewModels sufijo `ViewModel`, interfaces `I`+PascalCase, métodos async sufijo `Async`, campos privados `_camelCase`.
- `appsettings.json` es plantilla; los secretos reales en `appsettings.Development.json` (gitignored). Ver `../docs/SECURITY.md`.
- UI web: Bootstrap 5 (sin otros frameworks CSS), Bootstrap Icons para vistas nuevas, DataTables para tablas, estructura estándar `card > card-header bg-primary text-white / card-body`.

## Servicios principales (`Services/`)

- `TurnoService` — reglas de turnos (límite 2/mes por usuario y por documento, 30/día, slots Martes/Jueves 1–4 PM), hash SHA-256 de identidad, PDFs con iText7.
- `TurnoCleanupService` — BackgroundService horario: cancela turnos aprobados no atendidos.
- `PendingNotificationService` — notificaciones por polling (la app MAUI consulta cada 30 s `/api/notifications/pending` + heartbeat).
- `OneSignalNotificationService` (+ `NullOneSignalNotificationService` si no está configurado) — push opcional; FCM no funciona en Cuba.
- `BroadcastEmailProcessor` — cola persistente de emails masivos (`BroadcastEmailQueueItem`): 1 cada 60 s, tope 350/día (Gmail).
- `EmailService` / `EmailVerificationService` — SMTP; códigos de verificación de 6 dígitos (10 min de vida, reenvío mínimo 60 s).
- `ImageCompressionService` — SixLabors.ImageSharp: máx 1920x1080, calidad 85%.
- `MaintenanceService` — modo mantenimiento persistente en `maintenance.json`; `MaintenanceModeFilter` deja pasar solo Admin/Farmaceutico.

## Datos

- SQL Server vía EF Core; migraciones en `Migrations/` (**fuente de verdad**; los `.sql` de `../sql/` son parches históricos de producción).
- Seeding: `DbInitializer`/`DataSeeder` crean admin por defecto y datos de prueba en desarrollo.
- Integración externa: API CIMA (https://cima.aemps.es) para buscar medicamentos por código nacional (HttpClient con timeout 30 s).
- SignalR en `Hubs/` para notificaciones en tiempo real; los clientes caen automáticamente a polling/SSE si el host no soporta WebSocket.
