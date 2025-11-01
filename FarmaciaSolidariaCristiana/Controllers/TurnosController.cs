using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Controllers
{
    /// <summary>
    /// Controlador para gestión del Sistema de Turnos
    /// Roles: ViewerPublic (solicitar), Farmaceutico/Admin (gestionar)
    /// </summary>
    public class TurnosController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ITurnoService _turnoService;
        private readonly IEmailService _emailService;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<TurnosController> _logger;

        public TurnosController(
            ApplicationDbContext context,
            ITurnoService turnoService,
            IEmailService emailService,
            UserManager<IdentityUser> userManager,
            ILogger<TurnosController> logger)
        {
            _context = context;
            _turnoService = turnoService;
            _emailService = emailService;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Turnos
        /// <summary>
        /// Página principal de turnos - muestra información y estado de stock
        /// Accesible para todos los usuarios autenticados
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            
            // Si es ViewerPublic, mostrar sus turnos
            if (User.IsInRole("ViewerPublic"))
            {
                var userTurnos = await _turnoService.GetUserTurnosAsync(userId!);
                ViewData["UserTurnos"] = userTurnos;
                
                // Verificar si puede solicitar nuevo turno
                var (canRequest, reason) = await _turnoService.CanUserRequestTurnoAsync(userId!);
                ViewData["CanRequestTurno"] = canRequest;
                ViewData["CannotRequestReason"] = reason;
            }

            // Mostrar stock actual de medicamentos
            var medicinesWithStock = await _context.Medicines
                .Where(m => m.StockQuantity > 0)
                .OrderBy(m => m.Name)
                .ToListAsync();

            ViewData["AvailableMedicines"] = medicinesWithStock;

            return View();
        }

        // GET: Turnos/RequestForm
        /// <summary>
        /// Formulario para solicitar un turno
        /// Solo para usuarios con rol ViewerPublic
        /// </summary>
        [Authorize(Roles = "ViewerPublic")]
        public async Task<IActionResult> RequestForm()
        {
            var userId = _userManager.GetUserId(User);
            
            // Verificar si puede solicitar turno
            var (canRequest, reason) = await _turnoService.CanUserRequestTurnoAsync(userId!);
            
            if (!canRequest)
            {
                TempData["ErrorMessage"] = reason;
                return RedirectToAction(nameof(Index));
            }

            // Pasar medicamentos disponibles
            var medicines = await _context.Medicines
                .Where(m => m.StockQuantity > 0)
                .OrderBy(m => m.Name)
                .Select(m => new { m.Id, m.Name, m.StockQuantity, m.Unit })
                .ToListAsync();

            ViewBag.Medicines = medicines;

            // Pasar insumos disponibles
            var supplies = await _context.Supplies
                .Where(s => s.StockQuantity > 0)
                .OrderBy(s => s.Name)
                .Select(s => new { s.Id, s.Name, s.StockQuantity, s.Unit })
                .ToListAsync();

            ViewBag.Supplies = supplies;

            return View();
        }

        // POST: Turnos/RequestForm
        /// <summary>
        /// Procesa la solicitud de turno con uploads
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ViewerPublic")]
        public async Task<IActionResult> RequestForm(
            [Bind("DocumentoIdentidad,NotasSolicitante")] TurnoRequestViewModel model,
            List<int> medicineIds,
            List<int> quantities,
            List<int> supplyIds,
            List<int> supplyQuantities,
            IFormFile? receta,
            IFormFile? tarjeton)
        {
            try
            {
                // Validaciones básicas - debe haber medicamentos O insumos
                bool hasMedicines = medicineIds != null && medicineIds.Any();
                bool hasSupplies = supplyIds != null && supplyIds.Any();

                if (!hasMedicines && !hasSupplies)
                {
                    ModelState.AddModelError("", "Debe seleccionar al menos un medicamento o insumo");
                }

                if (hasMedicines && medicineIds.Count != quantities.Count)
                {
                    ModelState.AddModelError("", "Error en las cantidades de medicamentos");
                }

                if (hasSupplies && supplyIds.Count != supplyQuantities.Count)
                {
                    ModelState.AddModelError("", "Error en las cantidades de insumos");
                }

                // Validar uploads (receta opcional, tarjeton opcional)
                if (receta != null && receta.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("receta", "El archivo de receta no puede superar 5MB");
                }

                if (tarjeton != null && tarjeton.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("tarjeton", "El archivo del tarjetón no puede superar 5MB");
                }

                // Validar formatos de archivo
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                
                if (receta != null)
                {
                    var recetaExt = Path.GetExtension(receta.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(recetaExt))
                    {
                        ModelState.AddModelError("receta", "Solo se permiten archivos JPG, PNG o PDF");
                    }
                }

                if (tarjeton != null)
                {
                    var tarjetonExt = Path.GetExtension(tarjeton.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(tarjetonExt))
                    {
                        ModelState.AddModelError("tarjeton", "Solo se permiten archivos JPG, PNG o PDF");
                    }
                }

                if (!ModelState.IsValid)
                {
                    // Recargar medicamentos e insumos
                    var medicines = await _context.Medicines
                        .Where(m => m.StockQuantity > 0)
                        .OrderBy(m => m.Name)
                        .Select(m => new { m.Id, m.Name, m.StockQuantity, m.Unit })
                        .ToListAsync();
                    ViewBag.Medicines = medicines;

                    var supplies = await _context.Supplies
                        .Where(s => s.StockQuantity > 0)
                        .OrderBy(s => s.Name)
                        .Select(s => new { s.Id, s.Name, s.StockQuantity, s.Unit })
                        .ToListAsync();
                    ViewBag.Supplies = supplies;
                    
                    return View(model);
                }

                var userId = _userManager.GetUserId(User);

                // Crear lista de medicamentos solicitados
                var medicamentos = new List<(int MedicineId, int Quantity)>();
                if (medicineIds != null)
                {
                    for (int i = 0; i < medicineIds.Count; i++)
                    {
                        if (quantities[i] > 0)
                        {
                            medicamentos.Add((medicineIds[i], quantities[i]));
                        }
                    }
                }

                // Crear lista de insumos solicitados
                var insumos = new List<(int SupplyId, int Quantity)>();
                if (supplyIds != null)
                {
                    for (int i = 0; i < supplyIds.Count; i++)
                    {
                        if (supplyQuantities[i] > 0)
                        {
                            insumos.Add((supplyIds[i], supplyQuantities[i]));
                        }
                    }
                }

                // Crear turno (sin fecha, se asignará al aprobar)
                var turno = new Turno
                {
                    UserId = userId!,
                    DocumentoIdentidadHash = _turnoService.HashDocument(model.DocumentoIdentidad),
                    FechaPreferida = null, // Se asigna automáticamente al aprobar
                    NotasSolicitante = model.NotasSolicitante
                };

                var createdTurno = await _turnoService.CreateTurnoAsync(turno, medicamentos, insumos, receta, tarjeton);

                // Enviar email de confirmación al usuario (no bloqueante)
                var user = await _userManager.GetUserAsync(User);
                if (user?.Email != null)
                {
                    try
                    {
                        // Enviar email en segundo plano para no bloquear la respuesta
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _emailService.SendTurnoSolicitadoEmailAsync(user.Email, user.UserName ?? "Usuario");
                                _logger.LogInformation("Email de confirmación enviado a {Email} para turno {TurnoId}", user.Email, createdTurno.Id);
                            }
                            catch (Exception emailEx)
                            {
                                _logger.LogWarning(emailEx, "No se pudo enviar email de confirmación para turno {TurnoId}", createdTurno.Id);
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error iniciando envío de email para turno {TurnoId}", createdTurno.Id);
                    }
                }

                // Enviar notificación a farmacéuticos (NO en segundo plano para mantener contexto de BD)
                try
                {
                    var tipoSolicitud = medicamentos.Any() ? "Medicamentos" : "Insumos";
                    _logger.LogInformation("Iniciando envío de notificaciones a farmacéuticos para turno {TurnoId} (Tipo: {Tipo})", 
                        createdTurno.Id, tipoSolicitud);
                    var notificationSent = await _emailService.SendTurnoNotificationToFarmaceuticosAsync(
                        user?.UserName ?? "Usuario", 
                        createdTurno.Id,
                        tipoSolicitud);
                    
                    if (notificationSent)
                    {
                        _logger.LogInformation("✓ Notificaciones enviadas a farmacéuticos para turno {TurnoId}", createdTurno.Id);
                    }
                    else
                    {
                        _logger.LogWarning("⚠ No se pudieron enviar notificaciones a farmacéuticos para turno {TurnoId}", createdTurno.Id);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "✗ Error enviando notificaciones a farmacéuticos para turno {TurnoId}", createdTurno.Id);
                }

                TempData["SuccessMessage"] = "Tu solicitud de turno ha sido enviada exitosamente. " +
                    "Recibirás un email cuando sea revisada.";

                return RedirectToAction(nameof(Confirmation), new { id = createdTurno.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creando turno");
                ModelState.AddModelError("", $"Error al crear la solicitud: {ex.Message}");

                var medicines = await _context.Medicines
                    .Where(m => m.StockQuantity > 0)
                    .OrderBy(m => m.Name)
                    .Select(m => new { m.Id, m.Name, m.StockQuantity, m.Unit })
                    .ToListAsync();
                ViewBag.Medicines = medicines;

                var supplies = await _context.Supplies
                    .Where(s => s.StockQuantity > 0)
                    .OrderBy(s => s.Name)
                    .Select(s => new { s.Id, s.Name, s.StockQuantity, s.Unit })
                    .ToListAsync();
                ViewBag.Supplies = supplies;

                return View(model);
            }
        }

        // GET: Turnos/Confirmation/5
        /// <summary>
        /// Muestra confirmación de solicitud de turno
        /// </summary>
        [Authorize(Roles = "ViewerPublic")]
        public async Task<IActionResult> Confirmation(int id)
        {
            var turno = await _turnoService.GetTurnoByIdAsync(id);
            
            if (turno == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (turno.UserId != userId)
            {
                return Forbid();
            }

            return View(turno);
        }

        // GET: Turnos/Dashboard
        /// <summary>
        /// Dashboard para farmacéuticos - gestión de turnos
        /// </summary>
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Dashboard(string? estado, string? tipo, DateTime? desde, DateTime? hasta)
        {
            var turnos = await _turnoService.GetTurnosAsync(estado, desde, hasta);
            
            // Filtrar por tipo si se especificó
            if (!string.IsNullOrEmpty(tipo))
            {
                if (tipo == "Medicamentos")
                {
                    turnos = turnos.Where(t => t.Medicamentos != null && t.Medicamentos.Any()).ToList();
                }
                else if (tipo == "Insumos")
                {
                    turnos = turnos.Where(t => t.Insumos != null && t.Insumos.Any()).ToList();
                }
            }
            
            ViewData["EstadoFiltro"] = estado;
            ViewData["TipoFiltro"] = tipo;
            ViewData["DesdeFiltro"] = desde;
            ViewData["HastaFiltro"] = hasta;

            return View(turnos);
        }

        // GET: Turnos/Details/5
        /// <summary>
        /// Ver detalles de un turno
        /// </summary>
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Details(int id)
        {
            var turno = await _turnoService.GetTurnoByIdAsync(id);
            
            if (turno == null)
            {
                return NotFound();
            }

            return View(turno);
        }

        // POST: Turnos/Approve/5
        /// <summary>
        /// Aprueba un turno
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Approve(int id, string? comentarios)
        {
            var farmaceuticoId = _userManager.GetUserId(User);
            
            var (success, message, pdfPath) = await _turnoService.ApproveTurnoAsync(
                id, 
                farmaceuticoId!, 
                comentarios: comentarios
            );

            if (success)
            {
                TempData["SuccessMessage"] = "Turno aprobado exitosamente. El usuario recibirá un email de notificación.";
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // POST: Turnos/Reject/5
        /// <summary>
        /// Rechaza un turno
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Reject(int id, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["ErrorMessage"] = "Debe proporcionar un motivo para el rechazo";
                return RedirectToAction(nameof(Details), new { id });
            }

            var farmaceuticoId = _userManager.GetUserId(User);
            
            var (success, message) = await _turnoService.RejectTurnoAsync(id, farmaceuticoId!, motivo);

            if (success)
            {
                TempData["SuccessMessage"] = "Turno rechazado. El usuario recibirá un email con el motivo.";
            }
            else
            {
                TempData["ErrorMessage"] = message;
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // GET: Turnos/Verify
        /// <summary>
        /// Verificar turno por documento de identidad
        /// </summary>
        [Authorize(Roles = "Admin,Farmaceutico")]
        public IActionResult Verify()
        {
            return View();
        }

        // POST: Turnos/Verify
        /// <summary>
        /// Busca turno por documento
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Verify(string? documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
            {
                TempData["ErrorMessage"] = "Debe ingresar un número de documento";
                return View();
            }

            var documentHash = _turnoService.HashDocument(documento);
            var turno = await _turnoService.FindTurnoByDocumentHashAsync(documentHash);

            if (turno == null)
            {
                TempData["ErrorMessage"] = $"No se encontró ningún turno activo con el documento: {documento}";
                _logger.LogInformation("Turno search failed for document hash (not logging actual document for privacy)");
                return View();
            }

            // Redirigir a la vista con el turno encontrado
            return View(turno);
        }

        // POST: Turnos/Complete/5
        /// <summary>
        /// Marca un turno como completado (entregado)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Complete(int id)
        {
            var success = await _turnoService.CompleteTurnoAsync(id);

            if (success)
            {
                TempData["SuccessMessage"] = "Turno marcado como completado (entregado)";
            }
            else
            {
                TempData["ErrorMessage"] = "Error al completar el turno";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // GET: Turnos/CheckStock
        /// <summary>
        /// API endpoint para verificar stock en tiempo real
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<JsonResult> CheckStock(int medicineId)
        {
            var medicine = await _context.Medicines.FindAsync(medicineId);
            
            if (medicine == null)
            {
                return Json(new { available = false, stock = 0, message = "Medicamento no encontrado" });
            }

            return Json(new 
            { 
                available = medicine.StockQuantity > 0, 
                stock = medicine.StockQuantity,
                unit = medicine.Unit,
                name = medicine.Name
            });
        }

        // GET: Turnos/SearchMedicines
        /// <summary>
        /// API endpoint para búsqueda de medicamentos por texto (autocompletado)
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<JsonResult> SearchMedicines(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new { medicines = new List<object>() });
            }

            var medicines = await _context.Medicines
                .Where(m => m.Name.Contains(query))
                .OrderBy(m => m.Name)
                .Take(10)
                .Select(m => new
                {
                    id = m.Id,
                    name = m.Name,
                    stock = m.StockQuantity,
                    unit = m.Unit,
                    description = m.Description
                })
                .ToListAsync();

            return Json(new { medicines });
        }

        // GET: Turnos/SearchSupplies
        /// <summary>
        /// API endpoint para búsqueda de insumos médicos por texto (autocompletado)
        /// </summary>
        [HttpGet]
        [Authorize]
        public async Task<JsonResult> SearchSupplies(string query)
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                return Json(new { supplies = new List<object>() });
            }

            var supplies = await _context.Supplies
                .Where(s => s.Name.Contains(query))
                .OrderBy(s => s.Name)
                .Take(10)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    stock = s.StockQuantity,
                    unit = s.Unit,
                    description = s.Description
                })
                .ToListAsync();

            return Json(new { supplies });
        }
    }

    /// <summary>
    /// ViewModel para solicitud de turno
    /// </summary>
    public class TurnoRequestViewModel
    {
        [Required(ErrorMessage = "El Carnet de Identidad o Pasaporte es requerido")]
        [Display(Name = "Carnet de Identidad o Pasaporte")]
        [StringLength(20)]
        [RegularExpression(@"^(\d{11}|[A-Za-z]\d{6,7})$", 
            ErrorMessage = "Formato inválido. Use 11 dígitos para Carnet de Identidad o letra seguida de 6-7 dígitos para Pasaporte")]
        public string DocumentoIdentidad { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Las notas no pueden superar 1000 caracteres")]
        public string? NotasSolicitante { get; set; }
    }
}
