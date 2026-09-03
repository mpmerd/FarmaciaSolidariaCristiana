// Contexto completo de la app FarmaciaSolidariaCristiana para GitHub Copilot.
// Última actualización: abril 2026.
// Solución: FarmaciaSolidariaCristiana.sln con dos proyectos:
//   1. FarmaciaSolidariaCristiana  (ASP.NET Core 10 MVC + API)
//   2. FarmaciaSolidariaCristiana.Maui  (.NET MAUI Android - ya implementado)

// ===========================================
// DESCRIPCIÓN GENERAL DEL PROYECTO
// ===========================================
// Aplicación para gestionar una farmacia solidaria cristiana sin fines de lucro.
// Propósito: Facilitar la distribución gratuita de medicamentos e insumos médicos a personas necesitadas.
// Idioma de la UI: Español (cultura es-ES configurada globalmente).
// URL de producción: https://farmaciasolidaria.somee.com
// APK Android: https://farmaciasolidaria.somee.com/android/

// ===========================================
// ENTIDADES Y MODELOS PRINCIPALES
// ===========================================
//
// USUARIOS (ASP.NET Identity):
// - Roles: Admin (acceso total), Farmaceutico (gestión operativa), Viewer (solo lectura), ViewerPublic (solicitudes públicas de turnos).
// - Configuración de contraseñas: RequireDigit, RequireUppercase, RequireLowercase, RequireNonAlphanumeric, minLength 6.
// - Lockout: 5 intentos fallidos = bloqueo por 5 minutos.
// - Cookies de sesión con expiración de 8 horas y sliding expiration.
// - Registro con verificación de email: código de 6 dígitos enviado por SMTP, válido 10 minutos.
//
// MEDICAMENTOS (Medicine):
// - Atributos: Id, Name, Description, StockQuantity, Unit (default "comprimidos"), NationalCode (CN).
// - Búsqueda por código nacional vía API externa CIMA (Agencia Española de Medicamentos - https://cima.aemps.es/).
// - Relaciones: Colecciones de Deliveries y Donations.
//
// INSUMOS MÉDICOS (Supply):
// - Atributos: Id, Name, Description, StockQuantity, Unit (default "Unidades").
// - Similar a medicamentos pero para productos no farmacéuticos (vendas, gasas, jeringas, etc.).
//
// DONACIONES (Donation):
// - Atributos: MedicineId (opcional), SupplyId (opcional), Quantity, DonationDate, DonorNote, Comments.
// - Puede ser de medicamento O de insumo (polimorfismo simple).
// - Aumentan el stock automáticamente.
//
// ENTREGAS (Delivery):
// - Atributos: PatientIdentification (Carnet 11 dígitos o Pasaporte), MedicineId, SupplyId, PatientId, TurnoId, Quantity, DeliveryDate, CreatedAt.
// - Campos adicionales: PatientNote, Comments, Dosage, TreatmentDuration, BatchNumber, ExpiryDate, DeliveredBy.
// - Validación de identificación: Carnet cubano (11 dígitos) o Pasaporte (1-3 letras + 6-7 dígitos).
// - Reducen el stock automáticamente.
// - Pueden estar vinculadas a un Turno y/o Paciente.
//
// TURNOS/CITAS (Turno):
// - Sistema completo de solicitud de citas para retiro de medicamentos.
// - Atributos principales: UserId, DocumentoIdentidadHash (SHA-256), FechaPreferida, FechaSolicitud, Estado.
// - Estados posibles: Pendiente, Aprobado, Rechazado, Completado, Cancelado.
// - Horarios: Martes/Jueves 1-4 PM, slots cada 6 minutos (30 turnos/día máximo).
// - Sistema anti-abuso: Límite de 2 turnos por usuario por mes Y límite de 2 turnos por documento de identidad (paciente) por mes.
//   (Un usuario puede solicitar turnos para distintos pacientes, cada uno con su propio límite mensual.)
// - Documentos adjuntos: RecetaMedicaPath, TarjetonPath, colección de TurnoDocumento.
// - Campos de revisión: ComentariosFarmaceutico, RevisadoPorId, NotasSolicitante.
// - Relaciones: TurnoMedicamento (many-to-many), TurnoInsumo (many-to-many), TurnoDocumento (one-to-many).
//
// TURNO MEDICAMENTO (TurnoMedicamento):
// - Tabla intermedia Turno-Medicine con: CantidadSolicitada, CantidadAprobada, DisponibleAlSolicitar, Notas.
//
// TURNO INSUMO (TurnoInsumo):
// - Tabla intermedia Turno-Supply con: CantidadSolicitada, CantidadAprobada, DisponibleAlSolicitar, Notas.
//
// TURNO DOCUMENTO (TurnoDocumento):
// - Documentos adjuntos: DocumentType (Receta Médica, Tarjetón Sanitario, Informe Médico, Otro), FileName, FilePath, FileSize, ContentType, Description, UploadDate.
//
// PACIENTES (Patient):
// - Identificación: IdentificationDocument (Carnet/Pasaporte con misma validación que entregas).
// - Datos personales: FullName, Age, Gender (M/F), Address, Phone, Municipality, Province.
// - Datos clínicos: MainDiagnosis, AssociatedPathologies, KnownAllergies, CurrentTreatments, BloodPressure, Weight, Height.
// - Documentos adjuntos mediante PatientDocument (similar a TurnoDocumento).
//
// PATIENT DOCUMENT (PatientDocument):
// - Documentos del paciente: DocumentType (Receta Médica, Documento de Identidad, Evaluación Médica, Consentimiento, Tarjetón, Tratamiento).
// - Atributos: FileName, FilePath, FileSize, ContentType, Description, UploadDate.
//
// PATROCINADORES (Sponsor):
// - Atributos: Name, Description, LogoPath, IsActive, DisplayOrder, CreatedDate.
// - Logos mostrados en reportes PDF.
//
// FECHAS BLOQUEADAS (FechaBloqueada):
// - Para bloquear días donde no se permiten turnos (festivos, emergencias).
// - Atributos: Fecha, Motivo, UsuarioId, FechaCreacion.
//
// DECORACIONES NAVBAR (NavbarDecoration):
// - Sistema de decoraciones temáticas para el navbar (Navidad, Epifanía, etc.).
// - Tipos: Predefined (presets como navidad, epifania) o Custom.
// - Atributos: Name, Type, PresetKey, DisplayText, TextColor, CustomIconPath, IconClass, IconColor.
//
// NOTIFICACIONES PENDIENTES (PendingNotification):
// - Modelo para el sistema de notificaciones por polling (sustituto de push notifications).
// - Atributos: UserId, Title, Message, NotificationType, ReferenceId, ReferenceType, AdditionalData (JSON), IsRead, CreatedAt, ReadAt.
// - La app MAUI consulta periódicamente (cada 30 s) el endpoint de polling para recibir notificaciones.
// - Diseñado para Cuba donde Firebase Cloud Messaging (FCM) no está disponible.
//
// USER DEVICE TOKEN (UserDeviceToken):
// - Modelo para almacenar tokens de dispositivo OneSignal por usuario.
// - Atributos: UserId, OneSignalPlayerId, DeviceToken, DeviceType (iOS/Android), DeviceName, IsActive.
// - Un usuario puede tener múltiples dispositivos registrados.
// - Índice único en (UserId, OneSignalPlayerId).

// ===========================================
// SERVICIOS DE LA APLICACIÓN (backend)
// ===========================================
//
// EmailService (IEmailService):
// - Envío de emails vía SMTP (configurado en appsettings.json bajo SmtpSettings).
// - Soporta adjuntos y notificaciones HTML.
// - Notifica a usuarios sobre cambios en turnos, aprobaciones, rechazos.
//
// EmailVerificationService (IEmailVerificationService):
// - Genera y valida códigos de verificación de 6 dígitos para el registro de usuarios.
// - Almacenamiento en memoria con expiración de 10 minutos.
// - Rate limiting: no permite reenvío antes de 60 segundos.
// - Usa RandomNumberGenerator para seguridad criptográfica.
//
// TurnoService (ITurnoService):
// - Lógica de negocio para turnos: validación de límites (2/mes, 30/día), asignación de slots.
// - Hash SHA-256 para documentos de identidad.
// - Generación de PDFs de turnos con iText7.
//
// TurnoCleanupService (BackgroundService):
// - Servicio en segundo plano que se ejecuta cada hora.
// - Cancela automáticamente turnos aprobados donde el usuario no asistió.
//
// ImageCompressionService (IImageCompressionService):
// - Compresión de imágenes con SixLabors.ImageSharp.
// - Redimensiona a máximo 1920x1080, calidad 85%.
// - Soporta JPEG y PNG.
//
// MaintenanceService (IMaintenanceService):
// - Modo de mantenimiento persistente (archivo maintenance.json en ContentRootPath).
// - Permite activar/desactivar mantenimiento con motivo.
// - Durante mantenimiento: Admin y Farmaceutico pueden acceder, otros ven página de mantenimiento.
// - La app MAUI consulta GET /api/maintenanceapi/status (no requiere auth) al iniciar.
//
// PendingNotificationService (IPendingNotificationService):
// - Sistema de notificaciones por polling para la app MAUI (alternativa a push en Cuba).
// - CreateNotificationAsync, GetUnreadNotificationsAsync, GetNotificationsAsync(paginado).
// - MarkAsReadAsync, MarkAllAsReadAsync, GetUnreadCountAsync.
// - Accedido desde la API en GET /api/notifications/pending y POST /api/notifications/mark-read.
//
// OneSignalNotificationService (IOneSignalNotificationService):
// - Servicio para enviar notificaciones push usando OneSignal REST API.
// - NOTA: OneSignal es opcional. Si no está configurado, se usa NullOneSignalNotificationService.
// - Gestión de tokens: RegisterDeviceTokenAsync, UnregisterDeviceTokenAsync, GetUserDeviceTokensAsync.
// - Notificaciones de turnos: Solicitado, Aprobado, Rechazado, PdfDisponible, Recordatorio, Cancelado, Reprogramado.
// - SendNuevaSolicitudToFarmaceuticosAsync - Notifica a farmacéuticos sobre nuevas solicitudes.
// - Configuración en appsettings.json bajo "OneSignalSettings" (AppId, RestApiKey, ApiUrl).
//
// NullOneSignalNotificationService:
// - Implementación nula de IOneSignalNotificationService usada cuando OneSignal no está configurado.
// - Aún así crea PendingNotifications para el sistema de polling.
// - Registrada en DI condicionalmente según la configuración de OneSignal.

// ===========================================
// FILTROS Y MIDDLEWARE
// ===========================================
//
// MaintenanceModeFilter (IActionFilter):
// - Filtro global que redirige a página de mantenimiento si está activado.
// - Excepciones: Admin, Farmaceutico, y rutas de Account/Login.
//
// AppVersionCheckMiddleware:
// - Verifica el header X-App-Version en peticiones a /api/*.
// - Versión mínima requerida: 1.0.5 (constante MinimumAppVersion).
// - Retorna HTTP 426 (Upgrade Required) si la versión es inferior o si hay Bearer sin header de versión.
// - Peticiones sin Bearer y sin header se dejan pasar (login público, etc.).

// ===========================================
// CONTROLADORES MVC (Web)
// ===========================================
// - AccountController: Login, Logout, Register (con verificación email), gestión de usuarios y roles.
// - MedicinesController: CRUD de medicamentos, integración con API CIMA.
// - SuppliesController: CRUD de insumos médicos.
// - DonationsController: Registro de donaciones (aumenta stock).
// - DeliveriesController: Registro de entregas (reduce stock), vinculación con turnos/pacientes.
// - TurnosController: Solicitud, aprobación, rechazo de turnos. Sistema de citas completo.
// - PatientsController: CRUD de pacientes con historial médico.
// - ReportsController: Generación de PDFs (Entregas, Donaciones, Mensual con logos de sponsors).
// - SponsorsController: CRUD de patrocinadores con logos.
// - FechasBloqueadasController: Gestión de días sin turnos.
// - NavbarDecorationsController: Gestión de decoraciones temáticas.
// - MaintenanceController: Activar/desactivar modo mantenimiento.
// - BroadcastController (MVC): Envío masivo de mensajes a todos los usuarios (email + in-app). Solo Admin.
// - HomeController: Dashboard principal.

// ===========================================
// API RESTFUL (/Api/Controllers)
// ===========================================
// La aplicación incluye una API RESTful completa con autenticación JWT para la app MAUI.
//
// AUTENTICACIÓN JWT:
// - Configuración en appsettings.json bajo "JwtSettings" (SecretKey, Issuer, Audience, ExpirationMinutes).
// - Token expira en 8 horas por defecto (480 minutos).
// - Login en POST /api/auth/login retorna token JWT.
// - Header requerido en todos los requests autenticados: Authorization: Bearer {token}
// - Header de versión requerido: X-App-Version: {version} (mínimo 1.0.5)
//
// CONTROLADORES API (16 controladores):
// - ApiBaseController: Clase base abstracta con métodos helper (ApiOk, ApiError, ApiValidationError).
// - AuthController (api/auth): Login (/login), info usuario (/me), validar token (/validate), cambiar contraseña (/change-password).
// - MedicinesApiController (api/medicines): CRUD completo, GET /available, GET /cima/{cn} para búsqueda en API CIMA.
// - SuppliesApiController (api/supplies): CRUD completo, GET /available para stock disponible.
// - TurnosApiController (api/turnos): GET todos, GET /my, POST /{id}/approve, POST /{id}/reject, POST /{id}/complete, GET /stats.
// - DonationsApiController (api/donations): CRUD completo, GET /recent para últimos 30 días. Aumenta stock automáticamente.
// - DeliveriesApiController (api/deliveries): CRUD completo, GET /recent, GET /by-patient/{id}. Reduce stock automáticamente.
// - PatientsApiController (api/patients): CRUD completo, GET /by-identification/{id}, GET /{id}/deliveries para historial.
// - SponsorsApiController (api/sponsors): CRUD completo, GET /active para patrocinadores activos.
// - ReportsApiController (api/reports): POST /deliveries, POST /donations, POST /monthly (PDFs en Base64), GET /inventory, GET /dashboard (JSON).
// - NotificationsApiController (api/notifications): Sistema híbrido OneSignal + polling.
//   * POST /device - Registrar token OneSignal.
//   * POST /device/unregister - Eliminar registro de dispositivo.
//   * GET /devices - Listar dispositivos del usuario.
//   * GET /push-status - Estado de notificaciones push.
//   * POST /test - Enviar notificación de prueba.
//   * POST /send - Enviar notificación a usuario (Admin/Farmaceutico).
//   * POST /send/broadcast - Enviar a todos (Solo Admin).
//   * GET /pending - Obtener notificaciones no leídas (polling).
//   * POST /mark-read - Marcar notificaciones como leídas.
//   * POST /mark-all-read - Marcar todas como leídas.
//   * GET /unread-count - Número de notificaciones no leídas.
//   * POST /heartbeat - La app MAUI envía heartbeat cada 30s para detectar actividad.
// - FechasBloqueadasApiController (api/fechasbloqueadas): CRUD de fechas bloqueadas. Solo Admin.
// - MaintenanceApiController (api/maintenanceapi): GET /status - estado de mantenimiento (sin auth, para la app MAUI).
// - UsersApiController (api/users): Gestión de usuarios (solo Admin). GET todos, GET /{id}, POST, PUT /{id}/role, DELETE /{id}.
// - BroadcastController/Api (api/broadcast): POST /send - envío masivo (email + in-app). Solo Admin.
// - DiagnosticsController (api/diagnostics): Health checks. GET /ping, /config, /services, /database.
//
// MODELOS DTO (Api/Models - 10 archivos):
// - AuthDtos: LoginRequestDto, LoginResponseDto, UserDto, ChangePasswordDto, UserManagementDto.
// - MedicineDtos: MedicineDto, CreateMedicineDto, UpdateMedicineDto, PagedResult<T>, CimaMedicineDto.
// - SupplyDtos: SupplyDto, CreateSupplyDto, UpdateSupplyDto.
// - TurnoDtos: TurnoDto, TurnoMedicamentoDto, TurnoInsumoDto, ApproveTurnoDto, RejectTurnoDto, TurnoStatsDto, CanRequestTurnoDto.
// - DonationDtos: DonationDto, CreateDonationDto, UpdateDonationDto.
// - DeliveryDtos: DeliveryDto, CreateDeliveryDto, UpdateDeliveryDto.
// - PatientDtos: PatientDto, CreatePatientDto, UpdatePatientDto, PatientSummaryDto.
// - SponsorDtos: SponsorDto, CreateSponsorDto, UpdateSponsorDto.
// - ReportDtos: ReportResultDto (PDF en Base64), DeliveriesReportRequest, DonationsReportRequest, MonthlyReportRequest, DashboardStatsDto.
// - NotificationDtos: RegisterDeviceTokenDto, UnregisterDeviceTokenDto, DeviceTokenResponseDto, SendNotificationDto,
//                     NotificationResultDto, NotificationType (enum), PendingNotificationDto, BroadcastRequest,
//                     FechaBloqueadaDto, CreateFechaBloqueadaRequest.
//
// RESPUESTAS ESTANDARIZADAS:
// - Todas las respuestas usan ApiResponse<T> con: Success, Message, Data, Timestamp.
// - Paginación con PagedResult<T>: Items, Page, PageSize, TotalItems, TotalPages, HasPreviousPage, HasNextPage.
// - Reportes PDF retornados como Base64 en ReportResultDto.
//
// AUTORIZACIÓN API:
// - Todos los endpoints requieren JWT excepto /api/auth/login y /api/maintenanceapi/status.
// - Roles iguales a la web: Admin, Farmaceutico, Viewer, ViewerPublic.
// - [Authorize(Roles = "...")] en cada endpoint según permisos requeridos.

// ===========================================
// ARQUITECTURA TÉCNICA (Backend)
// ===========================================
// - Framework: ASP.NET Core 10 MVC + API RESTful.
// - ORM: Entity Framework Core con migraciones.
// - Base de datos: SQL Server (scripts en raíz del proyecto para migraciones manuales).
// - DbContext: ApplicationDbContext en /Data.
// - Autenticación Web: ASP.NET Identity con cookies (SameSite=Lax, SecurePolicy=Always).
// - Autenticación API: JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer).
// - Esquema de autenticación dual: cookies (Identity.Application) + JWT en controladores API.
// - Autorización: Por roles en controladores ([Authorize(Roles = "Admin,Farmaceutico")]).
// - UI: Razor Views con Bootstrap 5, DataTables para tablas, FontAwesome para iconos.
// - PDF: iText7 para generación de reportes.
// - Imágenes: SixLabors.ImageSharp para compresión.
// - HttpClient: Configurado para API CIMA con timeout de 30s.
// - Cultura: es-ES globalmente.
// - Notificaciones: Sistema de polling en DB + OneSignal opcional (FCM bloqueado en Cuba).

// ===========================================
// ESTRUCTURA DE CARPETAS (Backend)
// ===========================================
// /Controllers - Controladores MVC (14 archivos)
// /Api/Controllers - Controladores API RESTful (16 archivos):
//   ApiBaseController, AuthController, MedicinesApiController, SuppliesApiController,
//   TurnosApiController, DonationsApiController, DeliveriesApiController, PatientsApiController,
//   SponsorsApiController, ReportsApiController, NotificationsApiController,
//   FechasBloqueadasApiController, MaintenanceApiController, UsersApiController,
//   BroadcastController (API), DiagnosticsController
// /Api/Models - DTOs para la API (10 archivos)
// /Models - Entidades EF Core y ViewModels
//   Entidades: Delivery, Donation, FechaBloqueada, Medicine, NavbarDecoration, Patient,
//              PatientDocument, PendingNotification, Sponsor, Supply, Turno, TurnoDocumento,
//              TurnoInsumo, TurnoMedicamento, UserDeviceToken
//   /ViewModels: CreateUserViewModel, EditUserViewModel, LoginViewModel, RegisterViewModel
// /Views - Vistas Razor organizadas por controlador
// /Services - Servicios de negocio con interfaces (14 archivos):
//   EmailService, EmailVerificationService, ImageCompressionService, MaintenanceService,
//   NullOneSignalNotificationService, OneSignalNotificationService, PendingNotificationService,
//   TurnoCleanupService, TurnoService
// /Filters - MaintenanceModeFilter
// /Middleware - AppVersionCheckMiddleware
// /Data - ApplicationDbContext y configuraciones EF
// /Migrations - Migraciones EF Core
// /wwwroot - Archivos estáticos (CSS, JS, imágenes, uploads, /android/ para APK)
// /Properties - Configuración de lanzamiento

// ===========================================
// APP MAUI (FarmaciaSolidariaCristiana.Maui)
// ===========================================
// Proyecto .NET MAUI para Android — ya completamente implementado.
// Arquitectura: MVVM con CommunityToolkit.Mvvm, navegación con Shell.
//
// SERVICIOS MAUI:
// - ApiService (IApiService): Clase central que encapsula todos los llamados HTTP a la API.
//   * Gestiona el header X-App-Version automáticamente en cada request.
//   * Detecta respuesta 426 y muestra aviso de actualización.
//   * Detecta errores 401 y redirige al login.
// - AuthService (IAuthService): Gestión de sesión JWT con SecureStorage.
//   * GetTokenAsync, SaveTokenAsync, ClearTokenAsync.
//   * Almacena: token, userId, email, username, roles, expiración.
// - CacheService (ICacheService): Cache en memoria con TTL configurable (default 5 min).
//   * TryGet<T>, Set<T>, Invalidate.
// - PollingNotificationService (IPollingNotificationService): Polling de notificaciones cada 30 s.
//   * Llama a GET /api/notifications/pending y POST /api/notifications/heartbeat.
//   * Emite evento NotificationReceived con el objeto de notificación.
//   * Reproduce sonido al recibir notificación nueva.
// - NotificationService (INotificationService): Muestra notificaciones locales en Android.
// - UpdateService: Verifica actualizaciones disponibles consultando version.json.
//   * URL: https://farmaciasolidaria.somee.com/android/version.json
//   * CheckMaintenanceAsync(): consulta /api/maintenanceapi/status.
//   * CheckForUpdateAsync(): compara versión actual con la publicada.
// - ImageCompressionService: Redimensiona y comprime imágenes antes de subir.
//
// VIEWMODELS MAUI (21 archivos, todos heredan de BaseViewModel):
// - BaseViewModel: IsBusy, Title, comandos comunes.
// - LoginViewModel, RegisterViewModel, ChangePasswordViewModel, ProfileViewModel.
// - DashboardViewModel: Estadísticas del dashboard, accesos rápidos.
// - MedicamentosViewModel, InsumosViewModel.
// - DonacionesViewModel, NuevaDonacionViewModel.
// - EntregasViewModel, NuevaEntregaViewModel.
// - TurnosViewModel, SolicitarTurnoViewModel, ReprogramarTurnosViewModel.
// - PacientesViewModel, BloqueoPacienteViewModel.
// - PatrocinadoresViewModel, ReportesViewModel.
// - FechasBloqueadasViewModel, UsuariosViewModel.
//
// VISTAS MAUI (páginas implementadas):
// - LoginPage, RegisterPage, ChangePasswordPage, ProfilePage.
// - DashboardPage: Panel principal con accesos rápidos y contadores.
// - MedicamentosPage, InsumosPage.
// - DonacionesPage, NuevaDonacionPage.
// - EntregasPage, NuevaEntregaPage.
// - TurnosPage, SolicitarTurnoPage, ReprogramarTurnosPage.
// - PacientesPage, BloqueoPacientePage.
// - PatrocinadoresPage, ReportesPage, FechasBloqueadasPage.
// - UsuariosPage (solo Admin): lista y gestión de usuarios.
// - BroadcastPage (solo Admin): envío masivo de mensajes.
// - MaintenancePage: pantalla de mantenimiento mostrada al detectar modo mantenimiento.
// - AboutPage, PdfViewerPage, DocumentoViewerPage.
// - TextInputPopup: popup reutilizable para entrada de texto.
//
// MODELOS MAUI (/Models):
// - AuthModels.cs: LoginRequest, LoginResponse, UserInfo, ChangePasswordRequest.
// - EntityModels.cs: Mirrors de los DTOs del servidor (Medicine, Supply, Donation, Delivery,
//                    Patient, Sponsor, PendingNotification, UserManagement, etc.).
// - Turno.cs: Modelo de turno para la app (TurnoModel, TurnoMedicamento, TurnoInsumo).
//
// HELPERS MAUI:
// - Constants.cs: ApiBaseUrl (prod/debug), OneSignalAppId, SecureStorage keys, colores Bootstrap.
//
// CONVERTERS MAUI:
// - Converters.cs: InverseBoolConverter, IsNullConverter, StringToBoolConverter, etc.
//
// FLUJO DE INICIO DE LA APP:
// 1. App.xaml.cs llama UpdateService.CheckMaintenanceAsync() — si hay mantenimiento, navega a MaintenancePage.
// 2. UpdateService.CheckForUpdateAsync() — si hay actualización disponible, muestra diálogo.
// 3. AuthService verifica si hay token válido en SecureStorage.
// 4. Si autenticado → navega a DashboardPage; si no → navega a LoginPage.
// 5. PollingNotificationService.StartAsync() arranca en segundo plano.

// ===========================================
// DESPLIEGUE
// ===========================================
// - Scripts bash para Ubuntu (setup-ubuntu.sh), somee.com (deploy-to-somee.sh).
// - Guías de despliegue en archivos .md (DEPLOYMENT_UBUNTU.md, DEPLOYMENT_SOMEE.md).
// - Documentación API en API_DOCUMENTATION.md.
// - Configuración en appsettings.json y appsettings.Development.json.
// - APK publicado en wwwroot/android/ junto con version.json para auto-actualización.
//
// PRODUCCIÓN:
// - El servidor de producción es somee.com (https://farmaciasolidaria.somee.com).
// - Para desplegar el MVC/API usar el script deploy-to-somee.sh.
// - Para la app MAUI Android: generar el APK con generar_apk.sh, luego subirlo con subir_apk.sh.
// - NO se usa Google Play Store; la distribución del APK es directa mediante el servidor somee.com (wwwroot/android/).
// - El control de versiones de la app MAUI se gestiona vía version.json en el servidor, sin intervención de tiendas de apps.

// ===========================================
// INSTRUCCIONES PARA COPILOT
// ===========================================
// - El backend (API) y la app MAUI están completamente implementados.
// - Al generar código nuevo, mantén consistencia con los patrones existentes.
// - Backend: usa ApiResponse<T> en controladores API, hereda de ApiBaseController, usa ApiOk/ApiError.
// - MAUI: hereda ViewModels de BaseViewModel, usa IsBusy, usa IApiService para llamadas HTTP.
// - MAUI: usa Constants.ApiBaseUrl para la URL base; nunca hardcodees URLs.
// - Sistema de notificaciones: usa IPendingNotificationService en el backend; en MAUI, el polling
//   ya está implementado en PollingNotificationService.
// - Versión actual de la app MAUI: >= 1.0.6 (verificado por AppVersionCheckMiddleware).
// - Compatibilidad: .NET 10 (backend), .NET MAUI con workload net10.0-android (app móvil).

// ===========================================
// CONVENCIONES DE CODIFICACIÓN (C# / Backend)
// ===========================================
//
// NOMENCLATURA GENERAL:
// - Clases, interfaces, enums, métodos, propiedades: PascalCase.
//   Ejemplos: ApplicationDbContext, ITurnoService, TurnoCleanupService, ApiOk, StockQuantity.
// - Variables locales y parámetros: camelCase.
//   Ejemplos: patientId, fechaPreferida, turnoDto.
// - Campos privados: camelCase con prefijo underscore (_camelCase).
//   Ejemplos: _context, _emailService, _logger.
// - Constantes: PascalCase (si son públicas/protected) o SCREAMING_SNAKE_CASE solo si son privadas de bajo nivel.
//   Preferencia en este proyecto: PascalCase para constantes de clase.
// - Interfaces: prefijo I + PascalCase.  Ejemplo: IEmailService, ITurnoService.
// - DTOs (Data Transfer Objects): sufijo Dto.  Ejemplo: TurnoDto, CreateMedicineDto.
// - ViewModels: sufijo ViewModel.  Ejemplo: LoginViewModel, CreateUserViewModel.
// - Enums: PascalCase, valores en PascalCase.  Ejemplo: enum EstadoTurno { Pendiente, Aprobado, Rechazado }.
// - Archivos de configuración y scripts: kebab-case.  Ejemplo: appsettings.json, deploy-to-somee.sh.
//
// ESTRUCTURA DE CLASES:
// - Orden de miembros: campos estáticos → campos de instancia → constructores → propiedades → métodos públicos → métodos privados.
// - Un tipo principal por archivo; el nombre del archivo coincide con el tipo.
// - Usar record para DTOs inmutables si procede; class para entidades EF.
// - No omitir modificadores de acceso; siempre explícito (public, private, protected, internal).
//
// ASYNC / AWAIT:
// - Todos los métodos de servicio que tocan BD o HTTP son async Task<T>.
// - Nombrar métodos asíncronos con sufijo Async.  Ejemplo: GetUnreadNotificationsAsync.
// - Usar ConfigureAwait(false) en servicios de biblioteca; no es necesario en controladores ASP.NET Core.
//
// CONTROLADORES API:
// - Heredar de ApiBaseController.
// - Atributos de ruta en la clase: [Route("api/[controller]")].
// - Retornar siempre ApiResponse<T> usando los helpers ApiOk / ApiError / ApiValidationError.
// - Validar ModelState con if (!ModelState.IsValid) return ApiValidationError(...).
// - Try/catch en cada acción que acceda a BD, retornar ApiError con código 500 en excepciones inesperadas.
//
// CONTROLADORES MVC:
// - Heredar de Controller.
// - Usar TempData["SuccessMessage"] y TempData["ErrorMessage"] para feedback al usuario.
// - Redirigir con RedirectToAction tras POST exitoso (PRG pattern).
//
// ENTIDADES EF CORE:
// - Propiedades de navegación: virtual (para lazy loading si se activa) o cargadas con Include().
// - Claves foráneas: NombreEntidadId (int o string según Identity).
// - Auditoría mínima: CreatedAt (DateTime, UTC) donde aplique.
//
// INYECCIÓN DE DEPENDENCIAS:
// - Registrar servicios en Program.cs con el lifetime correcto:
//   AddScoped para servicios con estado por request, AddSingleton para servicios sin estado.
// - No usar ServiceLocator; inyectar siempre por constructor.
//
// VALIDACIÓN:
// - Usar Data Annotations en DTOs y ViewModels ([Required], [StringLength], [RegularExpression]).
// - Validar identificación cubana/pasaporte con el regex existente en el proyecto.
// - No duplicar lógica de validación; centralizar en el servicio cuando la validación es compleja.
//
// MANEJO DE ERRORES:
// - Loguear excepciones con ILogger<T> inyectado por constructor.
// - No exponer stack traces en respuestas de producción.
// - Usar mensajes de error en español (cultura es-ES).
//
// COMENTARIOS Y DOCUMENTACIÓN:
// - Usar /// <summary> en métodos y clases públicos de servicios y controladores.
// - Comentarios en español dentro del código.
// - No comentar código obvio; comentar el "por qué", no el "qué".

// ===========================================
// CONVENCIONES DE CODIFICACIÓN (MAUI / Mobile)
// ===========================================
//
// NOMENCLATURA:
// - ViewModels: sufijo ViewModel, heredan de BaseViewModel (CommunityToolkit.Mvvm).
// - Páginas (Views): sufijo Page.  Ejemplo: TurnosPage, SolicitarTurnoPage.
// - Servicios: sufijo Service, interfaz prefijo I.  Ejemplo: ApiService / IApiService.
// - Comandos: usar [RelayCommand] de CommunityToolkit.Mvvm; el método sin el sufijo Command.
//   Ejemplo: método CargarDatos() → comando CargarDatosCommand.
// - Propiedades observables: [ObservableProperty] (genera la propiedad con notificación).
//   El campo privado en camelCase sin underscore cuando se usa el generador de fuente.
//   Ejemplo: [ObservableProperty] string titulo; → genera public string Titulo { get; set; }
// - Colecciones: ObservableCollection<T> para listas enlazadas a la UI.
//
// PATRONES MVVM:
// - La lógica de negocio va en el ViewModel, NO en el code-behind de la Page.
// - El code-behind solo contiene inicialización, event handlers de ciclo de vida y navegación.
// - Usar IsBusy (heredado de BaseViewModel) para mostrar ActivityIndicator durante operaciones.
// - Usar IApiService para TODOS los llamados HTTP; nunca instanciar HttpClient directamente.
// - Usar Constants.ApiBaseUrl para la URL base; nunca hardcodear URLs.
//
// NAVEGACIÓN:
// - Shell navigation con rutas registradas en AppShell.xaml.
// - Pasar parámetros con QueryProperty o Shell.Current.GoToAsync con diccionario.
//
// ASYNC EN MAUI:
// - Los comandos [RelayCommand] usan métodos async Task.
// - Nunca bloquear el hilo principal; toda operación de red/BD es async.
// - Capturar excepciones en comandos y mostrar await DisplayAlert con mensaje en español.

// ===========================================
// PALETA DE COLORES Y ESTILOS UI
// ===========================================
//
// WEB (Bootstrap 5 — colores semánticos usados en el proyecto):
// - Primario (acciones principales, headers de cards):  bg-primary / btn-primary   → #0d6efd (azul Bootstrap)
// - Éxito (confirmaciones, stock disponible):           bg-success / btn-success   → #198754 (verde)
// - Peligro (eliminaciones, errores, rechazos):         bg-danger  / btn-danger    → #dc3545 (rojo)
// - Advertencia (alertas, turnos pendientes):           bg-warning / btn-warning   → #ffc107 (amarillo, texto dark)
// - Información (datos secundarios, info adicional):    bg-info    / btn-info      → #0dcaf0 (cyan)
// - Secundario (elementos neutros, badges de extra):    bg-secondary               → #6c757d (gris)
// - Claro (fondos, badges de nombre):                   bg-light text-dark         → #f8f9fa
// - Oscuro (números de turno, resaltados fuertes):      bg-dark                    → #212529
//
// SEMÁNTICA DE COLORES POR ESTADO DE TURNO:
// - Pendiente  → badge bg-warning text-dark
// - Aprobado   → badge bg-success
// - Rechazado  → badge bg-danger
// - Completado → badge bg-info
// - Cancelado  → badge bg-secondary
//
// REGLAS DE ESTILOS WEB:
// - Framework CSS: Bootstrap 5 (sin framework adicional; no agregar Tailwind u otros).
// - Iconos: Bootstrap Icons (bi bi-*) como clase principal; FontAwesome (fa fa-*) solo donde ya existe.
//   Preferir Bootstrap Icons para nuevas vistas.
// - Tablas: usar DataTables (ya cargado globalmente) con class="table table-striped table-hover".
// - Cards: estructura estándar card > card-header bg-primary text-white / card-body.
// - Formularios: usar form-floating para campos de texto cuando el espacio lo permite.
// - Alertas de feedback: alert alert-success / alert-danger con alert-dismissible fade show.
// - Tipografía: fuente del sistema (Bootstrap default); tamaño base 16px en ≥768px, 14px en móvil.
// - Espaciado: usar clases de utilidad Bootstrap (mt-*, mb-*, p-*); no CSS inline de espaciado.
// - Modales: Bootstrap Modal (data-bs-toggle="modal"); no librerías externas de modal.
//
// MAUI (colores definidos en Resources/Styles/Colors.xaml y Constants.cs):
// - Primary (botones, highlights):       #512BD4  (morado MAUI default — mantener coherencia con la app existente)
// - PrimaryDark (modo oscuro):           #ac99ea
// - Secondary (fondos suaves):           #DFD8F7
// - Tertiary:                            #2B0B98
// - Equivalencias Bootstrap usadas en la lógica (Constants.cs):
//     PrimaryColor  = #0d6efd   (azul — para badges/texto informativo)
//     SuccessColor  = #198754   (verde)
//     DangerColor   = #dc3545   (rojo)
//     WarningColor  = #ffc107   (amarillo)
//     InfoColor     = #0dcaf0   (cyan)
//     SecondaryColor= #6c757d   (gris)
//     LightColor    = #f8f9fa
//     DarkColor     = #212529
// - Grises de escala: Gray100 (#E1E1E1) a Gray950 (#141414).
// - Soporte light/dark con AppThemeBinding en todos los estilos XAML.
// - Fuente: OpenSansRegular / OpenSansSemibold (incluidas en Resources/Fonts).
// - Botones: CornerRadius=8, Padding=14,10, MinimumHeightRequest=44 (accesibilidad táctil).
// - NO hardcodear colores directamente en XAML; usar siempre StaticResource o AppThemeBinding.

// ===========================================
// PATRONES RECURRENTES — REFERENCIA RÁPIDA
// ===========================================
//
// NUEVO CONTROLADOR API:
//   [ApiController]
//   [Route("api/[controller]")]
//   [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
//   public class MiEntidadApiController : ApiBaseController { ... }
//
// NUEVA VISTA MVC (card estándar):
//   <div class="card shadow-sm">
//     <div class="card-header bg-primary text-white">
//       <h5 class="mb-0"><i class="bi bi-icon-name me-2"></i>Título</h5>
//     </div>
//     <div class="card-body"> ... </div>
//   </div>
//
// NUEVO VIEWMODEL MAUI:
//   public partial class MiViewModel : BaseViewModel
//   {
//       private readonly IApiService _apiService;
//       public MiViewModel(IApiService apiService) { _apiService = apiService; Title = "Título"; }
//       [RelayCommand] async Task CargarDatos() { if (IsBusy) return; try { IsBusy = true; ... } finally { IsBusy = false; } }
//   }
//
// NUEVA PAGE MAUI (mínimo):
//   public partial class MiPage : ContentPage
//   {
//       public MiPage(MiViewModel vm) { InitializeComponent(); BindingContext = vm; }
//   }
//
// FEEDBACK AL USUARIO (MVC):
//   TempData["SuccessMessage"] = "Operación realizada correctamente.";
//   TempData["ErrorMessage"]   = "Ocurrió un error al procesar la solicitud.";