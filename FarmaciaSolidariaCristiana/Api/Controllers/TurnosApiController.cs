using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Services;
using FarmaciaSolidariaCristiana.Api.Models;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// API para gestión de turnos/citas
    /// </summary>
    [Route("api/turnos")]
    public class TurnosApiController : ApiBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ITurnoService _turnoService;
        private readonly IOneSignalNotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly ILogger<TurnosApiController> _logger;
        private readonly IWebHostEnvironment _environment;

        public TurnosApiController(
            ApplicationDbContext context,
            ITurnoService turnoService,
            IOneSignalNotificationService notificationService,
            IEmailService emailService,
            ILogger<TurnosApiController> logger,
            IWebHostEnvironment environment)
        {
            _context = context;
            _turnoService = turnoService;
            _notificationService = notificationService;
            _emailService = emailService;
            _logger = logger;
            _environment = environment;
        }

        /// <summary>
        /// Obtiene todos los turnos (Admin/Farmaceutico)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<TurnoDto>>), 200)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20,
            [FromQuery] string? estado = null,
            [FromQuery] DateTime? fechaDesde = null,
            [FromQuery] DateTime? fechaHasta = null)
        {
            var query = _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .Include(t => t.Documentos)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(estado))
            {
                query = query.Where(t => t.Estado == estado);
            }

            if (fechaDesde.HasValue)
            {
                query = query.Where(t => t.FechaSolicitud >= fechaDesde.Value);
            }

            if (fechaHasta.HasValue)
            {
                query = query.Where(t => t.FechaSolicitud <= fechaHasta.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var turnos = await query
                .OrderByDescending(t => t.FechaSolicitud)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => MapToDto(t))
                .ToListAsync();

            return ApiOk(new PagedResult<TurnoDto>
            {
                Items = turnos,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Obtiene los turnos del usuario actual
        /// </summary>
        [HttpGet("my")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<List<TurnoDto>>), 200)]
        public async Task<IActionResult> GetMyTurnos()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var turnos = await _context.Turnos
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .Include(t => t.Documentos)
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.FechaSolicitud)
                .Select(t => MapToDto(t))
                .ToListAsync();

            return ApiOk(turnos);
        }

        /// <summary>
        /// Obtiene un turno por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<TurnoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Farmaceutico");

            var turno = await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .Include(t => t.Documentos)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (turno == null)
            {
                return ApiError("Turno no encontrado", 404);
            }

            // Verificar acceso: Admin/Farmaceutico pueden ver todos, usuarios solo los suyos
            if (!isAdmin && turno.UserId != userId)
            {
                return ApiError("No tiene permiso para ver este turno", 403);
            }

            return ApiOk(MapToDto(turno));
        }

        /// <summary>
        /// Obtiene los turnos aprobados de un paciente por su documento de identidad (para entregas)
        /// </summary>
        [HttpGet("by-identification/{identification}")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<List<TurnoForDeliveryDto>>), 200)]
        public async Task<IActionResult> GetByIdentification(string identification)
        {
            if (string.IsNullOrWhiteSpace(identification))
            {
                return ApiError("Identificación requerida");
            }

            // Normalizar documento
            identification = identification.Trim().ToUpper();
            
            // Calcular hash del documento
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(identification));
            var documentHash = Convert.ToBase64String(hashBytes);

            // Buscar turnos aprobados para este paciente
            var turnos = await _context.Turnos
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .Where(t => 
                    t.DocumentoIdentidadHash == documentHash && 
                    t.Estado == "Aprobado")
                .OrderByDescending(t => t.FechaPreferida ?? t.FechaSolicitud)
                .Select(t => new TurnoForDeliveryDto
                {
                    Id = t.Id,
                    NumeroTurno = t.NumeroTurno,
                    Estado = t.Estado,
                    FechaPreferida = t.FechaPreferida,
                    FechaSolicitud = t.FechaSolicitud,
                    Medicamentos = t.Medicamentos.Select(tm => new TurnoItemForDeliveryDto
                    {
                        Id = tm.MedicineId,
                        Nombre = tm.Medicine != null ? tm.Medicine.Name : "N/A",
                        CantidadSolicitada = tm.CantidadSolicitada,
                        CantidadAprobada = tm.CantidadAprobada,
                        Unidad = tm.Medicine != null ? tm.Medicine.Unit : "",
                        StockActual = tm.Medicine != null ? tm.Medicine.StockQuantity : 0,
                        Tipo = "Medicamento"
                    }).ToList(),
                    Insumos = t.Insumos.Select(ti => new TurnoItemForDeliveryDto
                    {
                        Id = ti.SupplyId,
                        Nombre = ti.Supply != null ? ti.Supply.Name : "N/A",
                        CantidadSolicitada = ti.CantidadSolicitada,
                        CantidadAprobada = ti.CantidadAprobada,
                        Unidad = ti.Supply != null ? ti.Supply.Unit : "",
                        StockActual = ti.Supply != null ? ti.Supply.StockQuantity : 0,
                        Tipo = "Insumo"
                    }).ToList()
                })
                .ToListAsync();

            return ApiOk(turnos);
        }

        /// <summary>
        /// Descarga el PDF de un turno aprobado
        /// </summary>
        [HttpGet("{id}/pdf")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            _logger.LogInformation("Solicitud de PDF para turno {TurnoId}", id);
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("Farmaceutico");

            _logger.LogInformation("Usuario {UserId}, IsAdmin: {IsAdmin}", userId, isAdmin);

            var turno = await _context.Turnos
                .FirstOrDefaultAsync(t => t.Id == id);

            if (turno == null)
            {
                _logger.LogWarning("Turno {TurnoId} no encontrado", id);
                return ApiError("Turno no encontrado", 404);
            }

            _logger.LogInformation("Turno encontrado. Estado: {Estado}, UserId del turno: {TurnoUserId}, TurnoPdfPath: {PdfPath}", 
                turno.Estado, turno.UserId, turno.TurnoPdfPath);

            // Verificar acceso: Admin/Farmaceutico pueden ver todos, usuarios solo los suyos
            if (!isAdmin && turno.UserId != userId)
            {
                _logger.LogWarning("Usuario {UserId} no tiene permiso para turno {TurnoId} que pertenece a {TurnoUserId}", 
                    userId, id, turno.UserId);
                return ApiError("No tiene permiso para acceder a este turno", 403);
            }

            // Verificar que el turno esté aprobado
            if (turno.Estado != "Aprobado")
            {
                _logger.LogWarning("Turno {TurnoId} no está aprobado, estado actual: {Estado}", id, turno.Estado);
                return ApiError("El PDF solo está disponible para turnos aprobados", 400);
            }

            // Verificar que existe el PDF
            if (string.IsNullOrEmpty(turno.TurnoPdfPath))
            {
                _logger.LogWarning("Turno {TurnoId} no tiene TurnoPdfPath", id);
                return ApiError("Este turno no tiene PDF generado", 404);
            }

            // Construir la ruta del archivo
            var pdfPath = Path.Combine(_environment.WebRootPath, turno.TurnoPdfPath.TrimStart('/'));
            _logger.LogInformation("Ruta del PDF: {PdfPath}", pdfPath);

            if (!System.IO.File.Exists(pdfPath))
            {
                _logger.LogWarning("PDF no encontrado en disco: {Path}", pdfPath);
                return ApiError("El archivo PDF no se encuentra en el servidor", 404);
            }

            // Leer y devolver el archivo
            var pdfBytes = await System.IO.File.ReadAllBytesAsync(pdfPath);
            var fileName = $"turno_{turno.NumeroTurno ?? turno.Id}.pdf";
            
            _logger.LogInformation("Enviando PDF {FileName}, tamaño: {Size} bytes", fileName, pdfBytes.Length);
            
            return File(pdfBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Verifica si el usuario puede solicitar un turno
        /// </summary>
        [HttpGet("can-request")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<CanRequestTurnoDto>), 200)]
        public async Task<IActionResult> CanRequestTurno()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var (canRequest, reason) = await _turnoService.CanUserRequestTurnoAsync(userId!);

            return ApiOk(new CanRequestTurnoDto
            {
                CanRequest = canRequest,
                Reason = reason
            });
        }

        /// <summary>
        /// Obtiene el próximo slot disponible
        /// </summary>
        [HttpGet("next-slot")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<DateTime>), 200)]
        public async Task<IActionResult> GetNextAvailableSlot()
        {
            var nextSlot = await _turnoService.GetNextAvailableSlotAsync();
            return ApiOk(nextSlot);
        }

        /// <summary>
        /// Crea una nueva solicitud de turno (para usuarios ViewerPublic desde app móvil)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<TurnoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> CreateTurno([FromBody] CreateTurnoApiDto model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();
                    return ApiError(string.Join("; ", errors));
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Verificar si el usuario puede solicitar turno (límite por usuario)
                var (canRequest, reason) = await _turnoService.CanUserRequestTurnoAsync(userId!);
                if (!canRequest)
                {
                    return ApiError(reason ?? "No puede solicitar turno en este momento");
                }

                // ✅ Verificar si el paciente (documento) puede recibir turno (límite por paciente)
                var (canPatientRequest, patientReason, patientTurnos) = await _turnoService.CanPatientRequestTurnoAsync(model.DocumentoIdentidad);
                if (!canPatientRequest)
                {
                    return ApiError(patientReason ?? "Este paciente ya alcanzó el límite de turnos mensuales");
                }

                // Validar que haya items
                if (model.Items == null || !model.Items.Any())
                {
                    return ApiError("Debe seleccionar al menos un medicamento o insumo");
                }

                // Crear listas de medicamentos o insumos según tipo
                var medicamentos = new List<(int MedicineId, int Quantity)>();
                var insumos = new List<(int SupplyId, int Quantity)>();

                if (model.TipoSolicitud == "Medicamento")
                {
                    foreach (var item in model.Items)
                    {
                        if (item.Cantidad > 0)
                        {
                            medicamentos.Add((item.Id, item.Cantidad));
                        }
                    }
                }
                else if (model.TipoSolicitud == "Insumo")
                {
                    foreach (var item in model.Items)
                    {
                        if (item.Cantidad > 0)
                        {
                            insumos.Add((item.Id, item.Cantidad));
                        }
                    }
                }

                // Crear turno
                var turno = new Turno
                {
                    UserId = userId!,
                    DocumentoIdentidadHash = _turnoService.HashDocument(model.DocumentoIdentidad),
                    FechaPreferida = null, // Se asigna al aprobar
                    NotasSolicitante = model.Notas
                };

                // Crear turno sin documentos (los documentos se suben en llamadas separadas)
                var createdTurno = await _turnoService.CreateTurnoWithDocumentsAsync(
                    turno, 
                    medicamentos, 
                    insumos, 
                    new List<IFormFile>(), 
                    new List<string>(), 
                    new List<string>());

                // Cargar datos completos para el DTO
                var turnoCompleto = await _context.Turnos
                    .Include(t => t.User)
                    .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                    .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                    .FirstOrDefaultAsync(t => t.Id == createdTurno.Id);

                _logger.LogInformation("Turno #{Id} creado vía API por usuario {UserId}", createdTurno.Id, userId);

                // Enviar notificación push/polling a farmacéuticos
                try
                {
                    var nombreUsuario = turnoCompleto?.User?.UserName ?? "Usuario";
                    var numeroTurno = turnoCompleto?.NumeroTurno ?? createdTurno.Id;
                    await _notificationService.SendNuevaSolicitudToFarmaceuticosAsync(
                        createdTurno.Id, 
                        numeroTurno, 
                        nombreUsuario);
                    _logger.LogInformation("Notificación push/polling enviada a farmacéuticos para turno #{TurnoId}", createdTurno.Id);
                }
                catch (Exception notifEx)
                {
                    // No fallar la solicitud si la notificación falla
                    _logger.LogWarning(notifEx, "Error enviando notificación push a farmacéuticos para turno #{TurnoId}", createdTurno.Id);
                }

                // Enviar email a farmacéuticos/admins que NO estén activos en la app móvil (usan web)
                try
                {
                    var nombreUsuario = turnoCompleto?.User?.UserName ?? "Usuario";
                    await _emailService.SendTurnoNotificationToFarmaceuticosAsync(
                        nombreUsuario, 
                        createdTurno.Id, 
                        "Nueva Solicitud");
                    _logger.LogInformation("Email enviado a farmacéuticos inactivos para turno #{TurnoId}", createdTurno.Id);
                }
                catch (Exception emailEx)
                {
                    _logger.LogWarning(emailEx, "Error enviando email a farmacéuticos para turno #{TurnoId}", createdTurno.Id);
                }

                return ApiOk(MapToDto(turnoCompleto!), "Turno solicitado exitosamente. Recibirás una notificación cuando sea revisado.");
            }
            catch (InvalidOperationException ex)
            {
                return ApiError(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando turno vía API");
                return ApiError("Error al crear la solicitud de turno");
            }
        }

        /// <summary>
        /// Sube un documento a un turno existente
        /// </summary>
        [HttpPost("{id}/documents")]
        [Authorize(Roles = "ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<TurnoDocumentoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> UploadDocument(int id, [FromForm] IFormFile file, [FromForm] string documentType, [FromForm] string? description)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Buscar turno
                var turno = await _context.Turnos
                    .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

                if (turno == null)
                {
                    return ApiError("Turno no encontrado", 404);
                }

                // Solo permitir subir documentos a turnos pendientes
                if (turno.Estado != EstadoTurno.Pendiente)
                {
                    return ApiError("Solo se pueden subir documentos a turnos pendientes");
                }

                // Validar archivo
                if (file == null || file.Length == 0)
                {
                    return ApiError("Debe seleccionar un archivo");
                }

                if (file.Length > 5 * 1024 * 1024)
                {
                    return ApiError("El archivo no puede superar 5MB");
                }

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(ext))
                {
                    return ApiError("Solo se permiten archivos JPG, PNG o PDF");
                }

                // Crear directorio
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "turnos");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Guardar archivo
                var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                long fileSize = file.Length;

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Crear registro
                var documento = new TurnoDocumento
                {
                    TurnoId = turno.Id,
                    DocumentType = documentType ?? "Otro",
                    FileName = file.FileName,
                    FilePath = $"/uploads/turnos/{fileName}",
                    FileSize = fileSize,
                    ContentType = file.ContentType,
                    Description = description,
                    UploadDate = DateTime.Now
                };

                _context.TurnoDocumentos.Add(documento);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Documento subido para turno #{TurnoId}: {FileName}", id, file.FileName);

                return ApiOk(new TurnoDocumentoDto
                {
                    Id = documento.Id,
                    DocumentType = documento.DocumentType,
                    FileName = documento.FileName,
                    FilePath = documento.FilePath,
                    FileSize = documento.FileSize,
                    ContentType = documento.ContentType,
                    Description = documento.Description,
                    UploadDate = documento.UploadDate
                }, "Documento subido exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo documento para turno #{Id}", id);
                return ApiError("Error al subir el documento");
            }
        }

        /// <summary>
        /// Aprueba un turno (Admin/Farmaceutico)
        /// </summary>
        [HttpPost("{id}/approve")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<TurnoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Approve(int id, [FromBody] ApproveTurnoDto? model = null)
        {
            var turno = await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (turno == null)
            {
                return ApiError("Turno no encontrado", 404);
            }

            if (turno.Estado != EstadoTurno.Pendiente)
            {
                return ApiError($"El turno no puede ser aprobado porque está en estado: {turno.Estado}");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Usar el servicio que genera el PDF y maneja todo correctamente
            var (success, message, pdfPath) = await _turnoService.ApproveTurnoAsync(
                id, 
                userId!, 
                null, // cantidadesAprobadas (se usan las solicitadas)
                model?.Comentarios);

            if (!success)
            {
                return ApiError(message ?? "Error al aprobar el turno");
            }

            // Recargar turno con datos actualizados
            turno = await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .FirstOrDefaultAsync(t => t.Id == id);

            _logger.LogInformation("Turno {Id} aprobado vía API por usuario {UserId}", id, userId);

            // La notificación (push o email) ya fue enviada por TurnoService.ApproveTurnoAsync

            return ApiOk(MapToDto(turno!), "Turno aprobado exitosamente");
        }

        /// <summary>
        /// Rechaza un turno (Admin/Farmaceutico)
        /// </summary>
        [HttpPost("{id}/reject")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<TurnoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Reject(int id, [FromBody] RejectTurnoDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Debe proporcionar un motivo de rechazo");
            }

            var turno = await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (turno == null)
            {
                return ApiError("Turno no encontrado", 404);
            }

            if (turno.Estado != EstadoTurno.Pendiente)
            {
                return ApiError($"El turno no puede ser rechazado porque está en estado: {turno.Estado}");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Usar el servicio para rechazar (maneja notificaciones push/email)
            var (success, message) = await _turnoService.RejectTurnoAsync(id, userId!, model.Motivo);

            if (!success)
            {
                return ApiError(message ?? "Error al rechazar el turno");
            }

            // Recargar turno con datos actualizados
            turno = await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .FirstOrDefaultAsync(t => t.Id == id);

            _logger.LogInformation("Turno {Id} rechazado vía API por usuario {UserId}", id, userId);

            // La notificación (push o email) ya fue enviada por TurnoService.RejectTurnoAsync

            return ApiOk(MapToDto(turno!), "Turno rechazado");
        }

        /// <summary>
        /// Cancela un turno por el paciente (debe faltar más de 7 días)
        /// </summary>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(typeof(ApiResponse<TurnoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Cancel(int id, [FromBody] CancelTurnoDto model)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model?.Motivo))
            {
                return ApiError("Debe proporcionar un motivo de cancelación");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var turno = await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (turno == null)
            {
                return ApiError("Turno no encontrado o no tiene permisos para cancelarlo", 404);
            }

            // Validar que se puede cancelar (debe ser Aprobado y faltar más de 7 días)
            if (!_turnoService.CanUserCancelTurno(turno))
            {
                var reason = _turnoService.GetCancelReasonMessage(turno);
                return ApiError(reason);
            }

            // Usar el servicio para cancelar (maneja devolución de stock y notificaciones push a farmacéuticos)
            var success = await _turnoService.CancelTurnoByUserAsync(id, userId!, model.Motivo);

            if (!success)
            {
                return ApiError("Error al cancelar el turno");
            }

            // Recargar turno con datos actualizados
            turno = await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .FirstOrDefaultAsync(t => t.Id == id);

            _logger.LogInformation("Turno {Id} cancelado por paciente vía API", id);

            // La notificación push a farmacéuticos ya fue enviada por TurnoService.CancelTurnoByUserAsync
            // El paciente recibirá confirmación local en la app (no push)

            return ApiOk(MapToDto(turno!), "Turno cancelado exitosamente");
        }

        /// <summary>
        /// Verifica si el usuario puede cancelar un turno específico
        /// </summary>
        [HttpGet("{id}/can-cancel")]
        [ProducesResponseType(typeof(ApiResponse<CanCancelTurnoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> CanCancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var turno = await _context.Turnos
                .FirstOrDefaultAsync(t => t.Id == id && t.UserId == userId);

            if (turno == null)
            {
                return ApiError("Turno no encontrado", 404);
            }

            var canCancel = _turnoService.CanUserCancelTurno(turno);
            var reason = canCancel ? null : _turnoService.GetCancelReasonMessage(turno);

            return ApiOk(new CanCancelTurnoDto
            {
                CanCancel = canCancel,
                Reason = reason,
                DiasRestantes = turno.FechaPreferida.HasValue 
                    ? (int)(turno.FechaPreferida.Value.Date - DateTime.Now.Date).TotalDays 
                    : 0
            });
        }

        /// <summary>
        /// Marca un turno como completado (Admin/Farmaceutico)
        /// </summary>
        [HttpPost("{id}/complete")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<TurnoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Complete(int id)
        {
            var turno = await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (turno == null)
            {
                return ApiError("Turno no encontrado", 404);
            }

            if (turno.Estado != EstadoTurno.Aprobado)
            {
                return ApiError($"El turno no puede ser completado porque está en estado: {turno.Estado}");
            }

            turno.Estado = EstadoTurno.Completado;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Turno {Id} completado vía API", id);

            return ApiOk(MapToDto(turno), "Turno completado exitosamente");
        }

        /// <summary>
        /// Reprograma un turno a una nueva fecha (Admin/Farmaceutico)
        /// </summary>
        [HttpPost("{id}/reschedule")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<TurnoDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Reschedule(int id, [FromBody] RescheduleTurnoDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            var turno = await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (turno == null)
            {
                return ApiError("Turno no encontrado", 404);
            }

            if (turno.Estado == EstadoTurno.Completado || turno.Estado == EstadoTurno.Cancelado)
            {
                return ApiError($"El turno no puede ser reprogramado porque está en estado: {turno.Estado}");
            }

            // Verificar que la fecha no esté bloqueada
            var fechaBloqueada = await _context.FechasBloqueadas
                .AnyAsync(f => f.Fecha.Date == model.NuevaFecha.Date);

            if (fechaBloqueada)
            {
                return ApiError("La fecha seleccionada está bloqueada");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var fechaAnterior = turno.FechaPreferida;

            turno.FechaPreferida = model.NuevaFecha.Date;
            turno.ComentariosFarmaceutico = model.Motivo ?? $"Reprogramado desde {fechaAnterior:dd/MM/yyyy}";
            turno.FechaRevision = DateTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Turno {Id} reprogramado de {FechaAnterior} a {NuevaFecha} por usuario {UserId}", 
                id, fechaAnterior, model.NuevaFecha, userId);

            return ApiOk(MapToDto(turno), "Turno reprogramado exitosamente");
        }

        /// <summary>
        /// Obtiene estadísticas de turnos
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<TurnoStatsDto>), 200)]
        public async Task<IActionResult> GetStats()
        {
            var hoy = DateTime.Today;
            var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

            var stats = new TurnoStatsDto
            {
                TotalPendientes = await _context.Turnos.CountAsync(t => t.Estado == EstadoTurno.Pendiente),
                TotalAprobados = await _context.Turnos.CountAsync(t => t.Estado == EstadoTurno.Aprobado),
                TotalCompletados = await _context.Turnos.CountAsync(t => t.Estado == EstadoTurno.Completado),
                TotalRechazados = await _context.Turnos.CountAsync(t => t.Estado == EstadoTurno.Rechazado),
                TurnosHoy = await _context.Turnos.CountAsync(t => t.FechaPreferida.HasValue && t.FechaPreferida.Value.Date == hoy),
                TurnosEsteMes = await _context.Turnos.CountAsync(t => t.FechaSolicitud >= inicioMes)
            };

            return ApiOk(stats);
        }

        #region Private Methods

        private static TurnoDto MapToDto(Turno t)
        {
            return new TurnoDto
            {
                Id = t.Id,
                UserId = t.UserId,
                UserEmail = t.User?.Email,
                NumeroTurno = t.NumeroTurno,
                FechaPreferida = t.FechaPreferida,
                FechaSolicitud = t.FechaSolicitud,
                Estado = t.Estado,
                NotasSolicitante = t.NotasSolicitante,
                ComentariosFarmaceutico = t.ComentariosFarmaceutico,
                FechaRevision = t.FechaRevision,
                TurnoPdfPath = t.TurnoPdfPath,
                Medicamentos = t.Medicamentos.Select(m => new TurnoMedicamentoDto
                {
                    MedicineId = m.MedicineId,
                    MedicineName = m.Medicine?.Name ?? "",
                    CantidadSolicitada = m.CantidadSolicitada,
                    CantidadAprobada = m.CantidadAprobada,
                    DisponibleAlSolicitar = m.DisponibleAlSolicitar
                }).ToList(),
                Insumos = t.Insumos.Select(i => new TurnoInsumoDto
                {
                    SupplyId = i.SupplyId,
                    SupplyName = i.Supply?.Name ?? "",
                    CantidadSolicitada = i.CantidadSolicitada,
                    CantidadAprobada = i.CantidadAprobada,
                    DisponibleAlSolicitar = i.DisponibleAlSolicitar
                }).ToList(),
                DocumentosCount = t.Documentos?.Count ?? 0,
                Documentos = t.Documentos?.Select(d => new TurnoDocumentoDto
                {
                    Id = d.Id,
                    DocumentType = d.DocumentType ?? "Otro",
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    FileSize = d.FileSize,
                    ContentType = d.ContentType,
                    Description = d.Description,
                    UploadDate = d.UploadDate
                }).ToList() ?? new List<TurnoDocumentoDto>(),
                CanceladoPorNoPresentacion = t.CanceladoPorNoPresentacion
            };
        }

        #endregion

        #region Reprogramación de Turnos

        /// <summary>
        /// Obtiene el conteo de turnos afectados en una fecha específica
        /// </summary>
        [HttpGet("reprogramar/preview")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<ReprogramarPreviewDto>), 200)]
        public async Task<IActionResult> GetReprogramarPreview([FromQuery] DateTime fecha)
        {
            var turnosAfectados = await _context.Turnos
                .Include(t => t.User)
                .Where(t => t.FechaPreferida.HasValue && 
                            t.FechaPreferida.Value.Date == fecha.Date &&
                            (t.Estado == EstadoTurno.Aprobado || 
                             t.Estado == EstadoTurno.Pendiente))
                .Select(t => new TurnoAfectadoDto
                {
                    Id = t.Id,
                    UserName = t.User != null ? t.User.UserName ?? "N/A" : "N/A",
                    UserEmail = t.User != null ? t.User.Email : null,
                    Estado = t.Estado,
                    FechaPreferida = t.FechaPreferida
                })
                .ToListAsync();

            return ApiOk(new ReprogramarPreviewDto
            {
                Fecha = fecha,
                TotalTurnos = turnosAfectados.Count,
                Turnos = turnosAfectados
            });
        }

        /// <summary>
        /// Reprograma todos los turnos de una fecha específica
        /// </summary>
        [HttpPost("reprogramar")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<ReprogramarResultDto>), 200)]
        public async Task<IActionResult> ReprogramarTurnos([FromBody] ReprogramarRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Motivo))
                return ApiError("Debe proporcionar un motivo para la reprogramación");

            // 1. Obtener turnos del día afectado (Aprobados y Pendientes)
            var turnosAfectados = await _context.Turnos
                .Include(t => t.User)
                .Include(t => t.Medicamentos).ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos).ThenInclude(ti => ti.Supply)
                .Where(t => t.FechaPreferida.HasValue && 
                            t.FechaPreferida.Value.Date == request.FechaAfectada.Date &&
                            (t.Estado == EstadoTurno.Aprobado || 
                             t.Estado == EstadoTurno.Pendiente))
                .ToListAsync();
            
            if (!turnosAfectados.Any())
                return ApiOk(new ReprogramarResultDto
                {
                    TotalAfectados = 0,
                    Reprogramados = 0,
                    NoReprogramados = 0,
                    Mensaje = "No hay turnos en esa fecha para reprogramar"
                });

            // 2. Por cada turno afectado, buscar próximo slot disponible
            var turnosReprogramados = new List<TurnoReprogramadoDto>();
            var turnosNoReprogramados = new List<TurnoNoReprogramadoDto>();
            
            foreach (var turno in turnosAfectados)
            {
                try
                {
                    // Buscar próxima fecha disponible (Martes o Jueves)
                    var nuevaFecha = await BuscarProximaFechaDisponibleAsync(request.FechaAfectada.AddDays(1));
                    
                    if (nuevaFecha == null)
                    {
                        turnosNoReprogramados.Add(new TurnoNoReprogramadoDto
                        {
                            TurnoId = turno.Id,
                            UserName = turno.User?.UserName ?? "N/A",
                            Razon = "No hay fechas disponibles en los próximos 60 días"
                        });
                        continue;
                    }
                    
                    // Buscar slot de hora disponible
                    var nuevoSlot = await BuscarProximoSlotDisponibleAsync(nuevaFecha.Value);
                    
                    if (nuevoSlot == null)
                    {
                        turnosNoReprogramados.Add(new TurnoNoReprogramadoDto
                        {
                            TurnoId = turno.Id,
                            UserName = turno.User?.UserName ?? "N/A",
                            Razon = "No hay slots de hora disponibles"
                        });
                        continue;
                    }
                    
                    var fechaOriginal = turno.FechaPreferida!.Value;
                    
                    // Actualizar turno
                    turno.FechaPreferida = nuevoSlot.Value;
                    turno.ComentariosFarmaceutico += $"\n[REPROGRAMADO - {DateTime.Now:dd/MM/yyyy HH:mm}]";
                    turno.ComentariosFarmaceutico += $"\nFecha original: {fechaOriginal:dd/MM/yyyy HH:mm}";
                    turno.ComentariosFarmaceutico += $"\nMotivo: {request.Motivo}";
                    
                    turnosReprogramados.Add(new TurnoReprogramadoDto
                    {
                        TurnoId = turno.Id,
                        UserName = turno.User?.UserName ?? "N/A",
                        UserEmail = turno.User?.Email,
                        FechaOriginal = fechaOriginal,
                        FechaNueva = nuevoSlot.Value
                    });
                    
                    _logger.LogInformation("Turno {TurnoId} reprogramado de {FechaAnterior} a {FechaNueva}",
                        turno.Id, fechaOriginal, nuevoSlot.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reprogramando turno {TurnoId}", turno.Id);
                    turnosNoReprogramados.Add(new TurnoNoReprogramadoDto
                    {
                        TurnoId = turno.Id,
                        UserName = turno.User?.UserName ?? "N/A",
                        Razon = $"Error: {ex.Message}"
                    });
                }
            }
            
            await _context.SaveChangesAsync();

            // Mensaje de resultado
            var mensaje = $"{turnosReprogramados.Count} turno(s) reprogramado(s) exitosamente.";
            if (turnosNoReprogramados.Any())
            {
                mensaje += $" {turnosNoReprogramados.Count} turno(s) NO pudieron reprogramarse.";
            }

            return ApiOk(new ReprogramarResultDto
            {
                TotalAfectados = turnosAfectados.Count,
                Reprogramados = turnosReprogramados.Count,
                NoReprogramados = turnosNoReprogramados.Count,
                Mensaje = mensaje,
                TurnosReprogramados = turnosReprogramados,
                TurnosNoReprogramados = turnosNoReprogramados
            });
        }

        /// <summary>
        /// Busca la próxima fecha disponible (Martes o Jueves, no bloqueada, con espacio)
        /// </summary>
        private async Task<DateTime?> BuscarProximaFechaDisponibleAsync(DateTime desde)
        {
            var fechaBusqueda = desde.Date;
            var diasBuscados = 0;
            
            while (diasBuscados < 60)
            {
                if (fechaBusqueda.DayOfWeek == DayOfWeek.Tuesday || 
                    fechaBusqueda.DayOfWeek == DayOfWeek.Thursday)
                {
                    var estaBloqueada = await _context.FechasBloqueadas
                        .AnyAsync(f => f.Fecha.Date == fechaBusqueda);
                    
                    if (!estaBloqueada)
                    {
                        var turnosEnFecha = await _context.Turnos
                            .CountAsync(t => t.FechaPreferida.HasValue &&
                                             t.FechaPreferida.Value.Date == fechaBusqueda &&
                                             (t.Estado == EstadoTurno.Aprobado || 
                                              t.Estado == EstadoTurno.Completado));
                        
                        if (turnosEnFecha < 30)
                        {
                            return fechaBusqueda;
                        }
                    }
                }
                
                fechaBusqueda = fechaBusqueda.AddDays(1);
                diasBuscados++;
            }
            
            return null;
        }

        /// <summary>
        /// Busca el próximo slot de hora disponible en una fecha específica
        /// </summary>
        private async Task<DateTime?> BuscarProximoSlotDisponibleAsync(DateTime fecha)
        {
            var horaInicio = new TimeSpan(13, 0, 0);
            var horaFin = new TimeSpan(16, 0, 0);
            var duracionSlot = TimeSpan.FromMinutes(6);
            
            var horaActual = horaInicio;
            
            while (horaActual < horaFin)
            {
                var fechaHora = fecha.Date.Add(horaActual);
                
                var ocupado = await _context.Turnos
                    .AnyAsync(t => t.FechaPreferida.HasValue &&
                                   t.FechaPreferida.Value == fechaHora &&
                                   (t.Estado == EstadoTurno.Aprobado || 
                                    t.Estado == EstadoTurno.Completado));
                
                if (!ocupado)
                {
                    return fechaHora;
                }
                
                horaActual = horaActual.Add(duracionSlot);
            }
            
            return null;
        }

        #endregion
    }

    // DTOs para Reprogramación
    public class ReprogramarRequest
    {
        public DateTime FechaAfectada { get; set; }
        public string Motivo { get; set; } = string.Empty;
    }

    public class ReprogramarPreviewDto
    {
        public DateTime Fecha { get; set; }
        public int TotalTurnos { get; set; }
        public List<TurnoAfectadoDto> Turnos { get; set; } = new();
    }

    public class TurnoAfectadoDto
    {
        public int Id { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaPreferida { get; set; }
    }

    public class ReprogramarResultDto
    {
        public int TotalAfectados { get; set; }
        public int Reprogramados { get; set; }
        public int NoReprogramados { get; set; }
        public string Mensaje { get; set; } = string.Empty;
        public List<TurnoReprogramadoDto> TurnosReprogramados { get; set; } = new();
        public List<TurnoNoReprogramadoDto> TurnosNoReprogramados { get; set; } = new();
    }

    public class TurnoReprogramadoDto
    {
        public int TurnoId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public DateTime FechaOriginal { get; set; }
        public DateTime FechaNueva { get; set; }
    }

    public class TurnoNoReprogramadoDto
    {
        public int TurnoId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Razon { get; set; } = string.Empty;
    }
}
