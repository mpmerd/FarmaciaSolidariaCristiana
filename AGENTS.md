# Farmacia Solidaria Cristiana — Guía para agentes de IA

Este archivo se carga automáticamente en cada sesión de opencode. Da el mapa general; el detalle está en archivos que debes leer bajo demanda (sección "Documentación de detalle").

## Qué es este proyecto

Sistema de gestión de una farmacia solidaria sin fines de lucro (Iglesia Metodista de Cárdenas y Adriano Solidario): distribución gratuita de medicamentos e insumos a personas necesitadas, con sistema de turnos (citas) para retirarlos.

- **Toda la UI y los mensajes de error van en español** (cultura es-ES global).
- Zona horaria de negocio: **America/Havana** (Cuba).
- Producción: https://farmaciasolidaria.somee.com — el APK se distribuye directo desde `wwwroot/android/`, sin Google Play.

## Solución (`FarmaciaSolidariaCristiana.sln`, .NET 10)

| Proyecto | Qué es |
|---|---|
| `FarmaciaSolidariaCristiana/` | Backend: ASP.NET Core 10 MVC (web de gestión) + API REST (`/api/*`) con JWT para la app móvil |
| `FarmaciaSolidariaCristiana.Maui/` | App Android .NET MAUI (MVVM + Shell) que consume la API |

## Comandos

### Backend (`cd FarmaciaSolidariaCristiana`)
```bash
dotnet build
dotnet run                        # http://localhost:5000
dotnet ef database update         # aplicar migraciones
dotnet ef migrations add <Nombre> # nueva migración
dotnet publish -c Release -o ./publish
```

### MAUI (`cd FarmaciaSolidariaCristiana.Maui`)
```bash
dotnet workload restore           # si falta el workload
dotnet build -f net10.0-android
dotnet publish -f net10.0-android -c Release   # genera APK
```

### Guard de verificación (correr al terminar cambios en C#)
```bash
bash scripts/check-no-datetime-now.sh
```

## Reglas críticas

1. **Nunca uses `DateTime.Now` ni `DateTime.Today`.** Todo timestamp de negocio sale de `CubaTime` (`Helpers/CubaTime.cs` de cada proyecto), calculado de UTC hacia America/Havana. `DateTime.UtcNow` solo para tokens JWT y PendingNotifications. El guard `scripts/check-no-datetime-now.sh` falla si hay usos fuera de la whitelist.
2. **Documentos de identidad** (carnet cubano de 11 dígitos o pasaporte): se almacenan como hash SHA-256 (`Turno.DocumentoIdentidadHash`), nunca en texto claro.
3. **Stock automático:** una Donación suma stock, una Entrega lo resta. No manipular `StockQuantity` fuera de la lógica de servicios correspondiente.
4. **Configuración/secretos:** `appsettings.json` es plantilla; los valores reales van en `appsettings.Development.json` (gitignored). Nunca hardcodear credenciales. Leer `docs/SECURITY.md` antes de tocar configuración.
5. **API móvil:** cada controlador API hereda de `ApiBaseController` y responde `ApiResponse<T>` (helpers `ApiOk`/`ApiError`/`ApiValidationError`). Se exige header `X-App-Version` (mínimo 1.0.5; si falta o es inferior → HTTP 426).

## Roles (idénticos en web y API)

`Admin` (todo) · `Farmaceutico` (CRUD operativo + gestión de turnos) · `Viewer` (solo lectura) · `ViewerPublic` (solicita turnos y consulta los suyos) · `ViewerPublicPlus` (como ViewerPublic pero hasta 10 turnos/mes; el límite de 2 por documento de identidad se mantiene). Autorización declarativa: `[Authorize(Roles = "...")]`.

## Mapa del backend (`FarmaciaSolidariaCristiana/`)

- `Controllers/` — MVC web (14 controladores; auth por cookies Identity).
- `Api/Controllers/` — API REST (16; rutas `/api/*`, JWT Bearer 8 h).
- `Api/Models/` — DTOs (sufijo `Dto`).
- `Models/` — entidades EF Core (+ `ViewModels/` para MVC).
- `Data/` — `ApplicationDbContext`, `DbInitializer`, `DataSeeder`.
- `Migrations/` — **fuente de verdad del esquema de BD**. Los `.sql` de `sql/` son parches históricos de producción; no usarlos como referencia del esquema actual.
- `Services/` — lógica de negocio (`TurnoService`, `PendingNotificationService`, `OneSignalNotificationService`, `EmailService`, `MaintenanceService`, `TurnoCleanupService`…).
- `Hubs/` — SignalR (notificaciones en tiempo real; fallback a polling).
- `Middleware/` — `AppVersionCheckMiddleware` (versión mínima de la app).
- `Program.cs` — única fuente de configuración: DI, Identity, JWT, auth dual (cookies web + Bearer API), HttpClient para CIMA.
- Detalle: leer `FarmaciaSolidariaCristiana/AGENTS.md`.

## Mapa MAUI (`FarmaciaSolidariaCristiana.Maui/`)

- MVVM (CommunityToolkit.Mvvm), Shell con Flyout, menús dinámicos según rol.
- `Services/ApiService.cs` — **todos** los HTTP (nunca `HttpClient` directo); maneja 401→login y 426→aviso de actualización.
- `Helpers/Constants.cs` — `ApiBaseUrl` y `OneSignalAppId` (dev/prod).
- JWT en `SecureStorage`; notificaciones por polling cada 30 s (FCM no disponible en Cuba).
- Detalle: leer `FarmaciaSolidariaCristiana.Maui/AGENTS.md`.

## Base de datos (SQL Server, EF Core)

17 entidades propias en `Models/` + tablas ASP.NET Identity. Núcleo: `Turno` (citas) con `TurnoMedicamento`/`TurnoInsumo` (N:M) y `TurnoDocumento`; `Donation`/`Delivery` (movimientos de stock); `Patient` + `PatientDocument`. Soporte: `Sponsor`, `FechaBloqueada`, `NavbarDecoration`, `PendingNotification`, `UserDeviceToken`, `BroadcastEmailQueueItem`.

Reglas de negocio clave: máximo 2 turnos/mes por usuario (10 si tiene rol `ViewerPublicPlus`) **y** 2 por documento de identidad; 30 turnos/día; horario de atención **Martes y Jueves** 1–4 PM (slots de 6 min); estados Pendiente → Aprobado/Rechazado → Completado/Cancelado.

**Esquema completo (entidades, relaciones, índices): `docs/BASE_DE_DATOS.md`.**

## Documentación de detalle (leer solo la que necesites)

| Archivo | Contenido |
|---|---|
| `docs/BASE_DE_DATOS.md` | Esquema y reglas de la BD |
| `docs/CONTEXTO_COMPLETO.md` | Contexto histórico completo del proyecto (servicios, DTOs, convenciones de código) |
| `docs/TURNOS_SYSTEM.md` | Sistema de turnos a fondo |
| `docs/API_DOCUMENTATION.md` | API REST |
| `docs/SECURITY.md` | Seguridad y credenciales |
| `docs/DEPLOYMENT_SOMEE.md`, `docs/DEPLOYMENT_UBUNTU.md` | Despliegue |
| `README.md` | Visión general del proyecto (para humanos) |
| `sql/` | Parches SQL históricos aplicados a producción |
| `scripts/` | Deploy, backup y guards de verificación |
