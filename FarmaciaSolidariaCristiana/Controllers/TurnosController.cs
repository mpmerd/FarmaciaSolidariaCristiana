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
            [Bind("DocumentoIdentidad,FechaPreferida,NotasSolicitante")] TurnoRequestViewModel model,
            List<int> medicineIds,
            List<int> quantities,
            IFormFile? receta,
            IFormFile? tarjeton)
        {
            try
            {
                // Validaciones básicas
                if (!medicineIds.Any() || medicineIds.Count != quantities.Count)
                {
                    ModelState.AddModelError("", "Debe seleccionar al menos un medicamento con cantidad");
                }

                // Validar fecha preferida
                if (model.FechaPreferida < DateTime.Now.AddHours(24))
                {
                    ModelState.AddModelError("FechaPreferida", "La fecha del turno debe ser al menos 24 horas en el futuro");
                }

                if (model.FechaPreferida > DateTime.Now.AddMonths(1))
                {
                    ModelState.AddModelError("FechaPreferida", "La fecha del turno no puede ser mayor a 1 mes");
                }

                // Validar uploads
                if (receta != null && receta.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("receta", "El archivo de receta no puede superar 5MB");
                }

                if (tarjeton == null || tarjeton.Length == 0)
                {
                    ModelState.AddModelError("tarjeton", "El documento de identidad (tarjetón/cédula) es obligatorio");
                }
                else if (tarjeton.Length > 5 * 1024 * 1024)
                {
                    ModelState.AddModelError("tarjeton", "El archivo del documento no puede superar 5MB");
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
                    // Recargar medicamentos
                    var medicines = await _context.Medicines
                        .Where(m => m.StockQuantity > 0)
                        .OrderBy(m => m.Name)
                        .Select(m => new { m.Id, m.Name, m.StockQuantity, m.Unit })
                        .ToListAsync();
                    ViewBag.Medicines = medicines;
                    
                    return View(model);
                }

                var userId = _userManager.GetUserId(User);

                // Crear lista de medicamentos solicitados
                var medicamentos = new List<(int MedicineId, int Quantity)>();
                for (int i = 0; i < medicineIds.Count; i++)
                {
                    if (quantities[i] > 0)
                    {
                        medicamentos.Add((medicineIds[i], quantities[i]));
                    }
                }

                // Crear turno
                var turno = new Turno
                {
                    UserId = userId!,
                    DocumentoIdentidadHash = _turnoService.HashDocument(model.DocumentoIdentidad),
                    FechaPreferida = model.FechaPreferida,
                    NotasSolicitante = model.NotasSolicitante
                };

                var createdTurno = await _turnoService.CreateTurnoAsync(turno, medicamentos, receta, tarjeton);

                // Enviar email de confirmación
                var user = await _userManager.GetUserAsync(User);
                if (user?.Email != null)
                {
                    await _emailService.SendTurnoSolicitadoEmailAsync(user.Email, user.UserName ?? "Usuario");
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
        public async Task<IActionResult> Dashboard(string? estado, DateTime? desde, DateTime? hasta)
        {
            var turnos = await _turnoService.GetTurnosAsync(estado, desde, hasta);
            
            ViewData["EstadoFiltro"] = estado;
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
        public async Task<IActionResult> Verify(string documento)
        {
            if (string.IsNullOrWhiteSpace(documento))
            {
                ModelState.AddModelError("documento", "Debe ingresar un número de documento");
                return View();
            }

            var documentHash = _turnoService.HashDocument(documento);
            var turno = await _turnoService.FindTurnoByDocumentHashAsync(documentHash);

            if (turno == null)
            {
                ViewData["NoFound"] = "No se encontró ningún turno activo con ese documento";
                return View();
            }

            return RedirectToAction(nameof(Details), new { id = turno.Id });
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
    }

    /// <summary>
    /// ViewModel para solicitud de turno
    /// </summary>
    public class TurnoRequestViewModel
    {
        [Required(ErrorMessage = "El documento de identidad es obligatorio")]
        [StringLength(20, ErrorMessage = "El documento no puede tener más de 20 caracteres")]
        public string DocumentoIdentidad { get; set; } = string.Empty;

        [Required(ErrorMessage = "La fecha preferida es obligatoria")]
        [DataType(DataType.DateTime)]
        public DateTime FechaPreferida { get; set; } = DateTime.Now.AddDays(2);

        [StringLength(1000, ErrorMessage = "Las notas no pueden superar 1000 caracteres")]
        public string? NotasSolicitante { get; set; }
    }
}
