using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Controllers
{
    /// <summary>
    /// Controlador para gestión de fechas bloqueadas
    /// Solo accesible para usuarios con rol Admin
    /// </summary>
    [Authorize(Roles = "Admin")]
    public class FechasBloqueadasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<FechasBloqueadasController> _logger;

        public FechasBloqueadasController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager,
            ILogger<FechasBloqueadasController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: FechasBloqueadas
        /// <summary>
        /// Muestra lista de fechas bloqueadas (futuras primero)
        /// </summary>
        public async Task<IActionResult> Index()
        {
            var hoy = DateTime.Now.Date;
            var fechasBloqueadas = await _context.FechasBloqueadas
                .Include(f => f.Usuario)
                .OrderBy(f => f.Fecha)
                .ToListAsync();

            return View(fechasBloqueadas);
        }

        // POST: FechasBloqueadas/Create
        /// <summary>
        /// Bloquea una fecha individual
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(DateTime fecha, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["ErrorMessage"] = "Debe proporcionar un motivo para el bloqueo.";
                return RedirectToAction(nameof(Index));
            }

            // Validar que la fecha sea futura
            if (fecha.Date < DateTime.Now.Date)
            {
                TempData["ErrorMessage"] = "Solo se pueden bloquear fechas futuras.";
                return RedirectToAction(nameof(Index));
            }

            // Validar que la fecha no esté ya bloqueada
            var yaExiste = await _context.FechasBloqueadas
                .AnyAsync(f => f.Fecha.Date == fecha.Date);

            if (yaExiste)
            {
                TempData["ErrorMessage"] = $"La fecha {fecha:dd/MM/yyyy} ya está bloqueada.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            var fechaBloqueada = new FechaBloqueada
            {
                Fecha = fecha.Date,
                Motivo = motivo,
                UsuarioId = userId!,
                FechaCreacion = DateTime.Now
            };

            _context.FechasBloqueadas.Add(fechaBloqueada);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Fecha bloqueada: {Fecha} por usuario {UserId}. Motivo: {Motivo}",
                fecha.Date, userId, motivo);

            TempData["SuccessMessage"] = $"Fecha {fecha:dd/MM/yyyy} bloqueada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: FechasBloqueadas/CreateRange
        /// <summary>
        /// Bloquea un rango de fechas
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRange(DateTime fechaInicio, DateTime fechaFin, string motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                TempData["ErrorMessage"] = "Debe proporcionar un motivo para el bloqueo.";
                return RedirectToAction(nameof(Index));
            }

            // Validar que ambas fechas sean futuras
            if (fechaInicio.Date < DateTime.Now.Date || fechaFin.Date < DateTime.Now.Date)
            {
                TempData["ErrorMessage"] = "Solo se pueden bloquear fechas futuras.";
                return RedirectToAction(nameof(Index));
            }

            // Validar que fechaFin >= fechaInicio
            if (fechaFin.Date < fechaInicio.Date)
            {
                TempData["ErrorMessage"] = "La fecha fin debe ser mayor o igual a la fecha inicio.";
                return RedirectToAction(nameof(Index));
            }

            // Validar rango máximo (30 días)
            var diasDiferencia = (fechaFin.Date - fechaInicio.Date).Days;
            if (diasDiferencia > 30)
            {
                TempData["ErrorMessage"] = "El rango máximo permitido es de 30 días.";
                return RedirectToAction(nameof(Index));
            }

            var userId = _userManager.GetUserId(User);
            var fechasAgregadas = 0;
            var fechasYaBloqueadas = new List<DateTime>();

            // Recorrer cada día del rango
            for (var fecha = fechaInicio.Date; fecha <= fechaFin.Date; fecha = fecha.AddDays(1))
            {
                // Verificar si ya está bloqueada
                var yaExiste = await _context.FechasBloqueadas
                    .AnyAsync(f => f.Fecha.Date == fecha);

                if (!yaExiste)
                {
                    var fechaBloqueada = new FechaBloqueada
                    {
                        Fecha = fecha,
                        Motivo = motivo,
                        UsuarioId = userId!,
                        FechaCreacion = DateTime.Now
                    };

                    _context.FechasBloqueadas.Add(fechaBloqueada);
                    fechasAgregadas++;
                }
                else
                {
                    fechasYaBloqueadas.Add(fecha);
                }
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Rango de fechas bloqueadas: {Inicio} a {Fin} por usuario {UserId}. " +
                "Agregadas: {Agregadas}, Ya existían: {Existentes}",
                fechaInicio.Date, fechaFin.Date, userId, fechasAgregadas, fechasYaBloqueadas.Count);

            if (fechasAgregadas > 0)
            {
                var mensaje = $"{fechasAgregadas} fecha(s) bloqueada(s) exitosamente.";
                if (fechasYaBloqueadas.Any())
                {
                    mensaje += $" {fechasYaBloqueadas.Count} fecha(s) ya estaban bloqueadas.";
                }
                TempData["SuccessMessage"] = mensaje;
            }
            else
            {
                TempData["ErrorMessage"] = "Todas las fechas del rango ya estaban bloqueadas.";
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: FechasBloqueadas/Delete/5
        /// <summary>
        /// Desbloquea una fecha
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var fechaBloqueada = await _context.FechasBloqueadas.FindAsync(id);
            
            if (fechaBloqueada == null)
            {
                TempData["ErrorMessage"] = "Fecha bloqueada no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            _context.FechasBloqueadas.Remove(fechaBloqueada);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Fecha desbloqueada: {Fecha} por usuario {UserId}",
                fechaBloqueada.Fecha, _userManager.GetUserId(User));

            TempData["SuccessMessage"] = $"Fecha {fechaBloqueada.Fecha:dd/MM/yyyy} desbloqueada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // POST: FechasBloqueadas/DeletePast
        /// <summary>
        /// Elimina todas las fechas bloqueadas pasadas (limpieza)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePast()
        {
            var hoy = DateTime.Now.Date;
            var fechasPasadas = await _context.FechasBloqueadas
                .Where(f => f.Fecha < hoy)
                .ToListAsync();

            if (!fechasPasadas.Any())
            {
                TempData["InfoMessage"] = "No hay fechas pasadas para eliminar.";
                return RedirectToAction(nameof(Index));
            }

            _context.FechasBloqueadas.RemoveRange(fechasPasadas);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Eliminadas {Count} fechas bloqueadas pasadas por usuario {UserId}",
                fechasPasadas.Count, _userManager.GetUserId(User));

            TempData["SuccessMessage"] = $"{fechasPasadas.Count} fecha(s) pasada(s) eliminada(s).";
            return RedirectToAction(nameof(Index));
        }
    }
}
