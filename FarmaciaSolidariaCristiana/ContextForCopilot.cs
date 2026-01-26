// Prompt para GitHub Copilot: Explicación completa del contexto de la app FarmaciaSolidariaCristiana para generar una API y app MAUI.

// ===========================================
// DESCRIPCIÓN GENERAL DEL PROYECTO
// ===========================================
// Aplicación web MVC en ASP.NET Core 8 para gestionar una farmacia solidaria cristiana sin fines de lucro.
// Propósito: Facilitar la distribución gratuita de medicamentos e insumos médicos a personas necesitadas.
// Idioma de la UI: Español (cultura es-ES configurada globalmente).

// ===========================================
// ENTIDADES Y MODELOS PRINCIPALES
// ===========================================
// 
// USUARIOS (ASP.NET Identity):
// - Roles: Admin (acceso total), Farmaceutico (gestión operativa), Viewer (solo lectura), ViewerPublic (solicitudes públicas de turnos).
// - Configuración de contraseñas: RequireDigit, RequireUppercase, RequireLowercase, RequireNonAlphanumeric, minLength 6.
// - Lockout: 5 intentos fallidos = bloqueo por 5 minutos.
// - Cookies de sesión con expiración de 8 horas y sliding expiration.
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
// - Sistema anti-abuso: Límite de 2 turnos por usuario por mes.
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

// ===========================================
// SERVICIOS DE LA APLICACIÓN
// ===========================================
//
// EmailService (IEmailService):
// - Envío de emails vía SMTP (configurado en appsettings.json bajo SmtpSettings).
// - Soporta adjuntos y notificaciones HTML.
// - Notifica a usuarios sobre cambios en turnos, aprobaciones, rechazos.
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
//
// OneSignalNotificationService (IOneSignalNotificationService):
// - Servicio para enviar notificaciones push usando OneSignal REST API.
// - IMPORTANTE: Se usa OneSignal porque Firebase Cloud Messaging (FCM) no está disponible en Cuba.
// - Gestión de tokens de dispositivo: RegisterDeviceTokenAsync, UnregisterDeviceTokenAsync, GetUserDeviceTokensAsync.
// - Envío de notificaciones: SendNotificationToUserAsync, SendNotificationToPlayersAsync, SendNotificationToAllAsync.
// - Notificaciones específicas de turnos:
//   * SendTurnoSolicitadoNotificationAsync - Notifica que el turno fue solicitado.
//   * SendTurnoAprobadoNotificationAsync - Notifica aprobación con fecha/hora y PDF.
//   * SendTurnoRechazadoNotificationAsync - Notifica rechazo con motivo.
//   * SendTurnoPdfDisponibleNotificationAsync - Notifica que el PDF está listo.
//   * SendTurnoRecordatorioNotificationAsync - Recordatorio de cita próxima.
//   * SendTurnoCanceladoNotificationAsync - Notifica cancelación con motivo.
//   * SendTurnoReprogramadoNotificationAsync - Notifica cambio de fecha.
//   * SendNuevaSolicitudToFarmaceuticosAsync - Notifica a farmacéuticos sobre nuevas solicitudes.
// - Configuración en appsettings.json bajo "OneSignalSettings" (AppId, RestApiKey, ApiUrl).
//
// USER DEVICE TOKEN (UserDeviceToken):
// - Modelo para almacenar tokens de dispositivo OneSignal por usuario.
// - Atributos: UserId, OneSignalPlayerId, DeviceToken, DeviceType (iOS/Android), DeviceName, IsActive.
// - Un usuario puede tener múltiples dispositivos registrados.
// - Índice único en (UserId, OneSignalPlayerId).

// ===========================================
// FILTROS Y MIDDLEWARE
// ===========================================
//
// MaintenanceModeFilter (IActionFilter):
// - Filtro global que redirige a página de mantenimiento si está activado.
// - Excepciones: Admin, Farmaceutico, y rutas de Account/Login.

// ===========================================
// CONTROLADORES MVC (Web)
// ===========================================
// - AccountController: Login, Logout, Register, gestión de usuarios y roles.
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
// - HomeController: Dashboard principal.

// ===========================================
// API RESTFUL (/Api/Controllers)
// ===========================================
// La aplicación incluye una API RESTful completa con autenticación JWT para ser consumida por apps .NET MAUI.
//
// AUTENTICACIÓN JWT:
// - Configuración en appsettings.json bajo "JwtSettings" (SecretKey, Issuer, Audience, ExpirationMinutes).
// - Token expira en 8 horas por defecto (480 minutos).
// - Login en POST /api/auth/login retorna token JWT.
//
// CONTROLADORES API (10 controladores):
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
// - NotificationsApiController (api/notifications): Notificaciones push con OneSignal.
//   * POST /device - Registrar token de dispositivo OneSignal.
// - DiagnosticsController (api/diagnostics): Health checks y diagnóstico de la API.
//   * GET /ping - Verificar que la API está funcionando.
//   * GET /config - Verificar configuración (JWT, DB, OneSignal).
//   * GET /services - Verificar servicios DI registrados.
//   * GET /database - Verificar conexión a base de datos.
//   * POST /device/unregister - Eliminar registro de dispositivo (logout).
//   * GET /devices - Listar dispositivos del usuario.
//   * GET /push-status - Estado de notificaciones push.
//   * POST /test - Enviar notificación de prueba.
//   * POST /send - Enviar notificación a usuario (Admin/Farmaceutico).
//   * POST /send/broadcast - Enviar a todos (Solo Admin).
//
// MODELOS DTO (Api/Models - 10 archivos):
// - AuthDtos: LoginRequestDto, LoginResponseDto, UserDto, ChangePasswordDto.
// - MedicineDtos: MedicineDto, CreateMedicineDto, UpdateMedicineDto, PagedResult<T>, CimaMedicineDto.
// - SupplyDtos: SupplyDto, CreateSupplyDto, UpdateSupplyDto.
// - TurnoDtos: TurnoDto, TurnoMedicamentoDto, TurnoInsumoDto, ApproveTurnoDto, RejectTurnoDto, TurnoStatsDto, CanRequestTurnoDto.
// - DonationDtos: DonationDto, CreateDonationDto, UpdateDonationDto.
// - DeliveryDtos: DeliveryDto, CreateDeliveryDto, UpdateDeliveryDto.
// - PatientDtos: PatientDto, CreatePatientDto, UpdatePatientDto, PatientSummaryDto.
// - SponsorDtos: SponsorDto, CreateSponsorDto, UpdateSponsorDto.
// - ReportDtos: ReportResultDto (PDF en Base64), DeliveriesReportRequest, DonationsReportRequest, MonthlyReportRequest, DashboardStatsDto.
// - NotificationDtos: RegisterDeviceTokenDto, UnregisterDeviceTokenDto, DeviceTokenResponseDto, SendNotificationDto, NotificationResultDto, NotificationType (enum).
//
// RESPUESTAS ESTANDARIZADAS:
// - Todas las respuestas usan ApiResponse<T> con: Success, Message, Data, Timestamp.
// - Paginación con PagedResult<T>: Items, Page, PageSize, TotalItems, TotalPages, HasPreviousPage, HasNextPage.
// - Reportes PDF retornados como Base64 en ReportResultDto.
//
// AUTORIZACIÓN API:
// - Todos los endpoints requieren JWT excepto /api/auth/login.
// - Roles iguales a la web: Admin, Farmaceutico, Viewer, ViewerPublic.
// - [Authorize(Roles = "...")] en cada endpoint según permisos requeridos.

// ===========================================
// ARQUITECTURA TÉCNICA
// ===========================================
// - Framework: ASP.NET Core 8 MVC + API RESTful.
// - ORM: Entity Framework Core con migraciones.
// - Base de datos: SQL Server (scripts en raíz del proyecto para migraciones manuales).
// - DbContext: ApplicationDbContext en /Data.
// - Autenticación Web: ASP.NET Identity con cookies (SameSite=Lax, SecurePolicy=Always).
// - Autenticación API: JWT Bearer (Microsoft.AspNetCore.Authentication.JwtBearer).
// - Autorización: Por roles en controladores ([Authorize(Roles = "Admin,Farmaceutico")]).
// - UI: Razor Views con Bootstrap 5, DataTables para tablas, FontAwesome para iconos.
// - PDF: iText7 para generación de reportes.
// - Imágenes: SixLabors.ImageSharp para compresión.
// - HttpClient: Configurado para API CIMA con timeout de 30s.
// - Cultura: es-ES globalmente.

// ===========================================
// ESTRUCTURA DE CARPETAS
// ===========================================
// /Controllers - Controladores MVC (web)
// /Api/Controllers - Controladores API RESTful (10 archivos):
//   ApiBaseController, AuthController, MedicinesApiController, SuppliesApiController,
//   TurnosApiController, DonationsApiController, DeliveriesApiController, 
//   PatientsApiController, SponsorsApiController, ReportsApiController
// /Api/Models - DTOs para la API (9 archivos):
//   AuthDtos, MedicineDtos, SupplyDtos, TurnoDtos, DonationDtos, 
//   DeliveryDtos, PatientDtos, SponsorDtos, ReportDtos
// /Models - Entidades EF Core y ViewModels
// /Views - Vistas Razor organizadas por controlador
// /Services - Servicios de negocio con interfaces
// /Filters - Filtros de acción (MaintenanceModeFilter)
// /Data - ApplicationDbContext y configuraciones EF
// /Migrations - Migraciones EF Core
// /wwwroot - Archivos estáticos (CSS, JS, imágenes, uploads)
// /Properties - Configuración de lanzamiento

// ===========================================
// DESPLIEGUE
// ===========================================
// - Scripts bash para Ubuntu (setup-ubuntu.sh), somee.com (deploy-to-somee.sh).
// - Guías de despliegue en archivos .md (DEPLOYMENT_UBUNTU.md, DEPLOYMENT_SOMEE.md).
// - Documentación API en API_DOCUMENTATION.md.
// - Configuración en appsettings.json y appsettings.Development.json.

// ===========================================
// PLAN DE EXTENSIÓN - APP MAUI
// ===========================================
// La API RESTful ya está completamente implementada. El siguiente paso es crear un proyecto .NET MAUI separado:
// 1. Crear proyecto MAUI para Android/iOS.
// 2. Consumir la API con HttpClient usando los DTOs definidos.
// 3. Implementar autenticación JWT con almacenamiento seguro del token.
// 4. Replicar pantallas: Login, Dashboard, CRUD de medicamentos/turnos, reportes, etc.
// 5. Usar patrón MVVM con CommunityToolkit.Mvvm.
// 6. Implementar navegación con Shell.

// Instrucciones para Copilot: Usa este contexto para generar código coherente con la app existente. 
// La API ya está implementada - genera código cliente MAUI que la consuma. Asegura compatibilidad con .NET 8.