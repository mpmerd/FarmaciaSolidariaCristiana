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
        private readonly ILogger<TurnosApiController> _logger;

        public TurnosApiController(
            ApplicationDbContext context,
            ITurnoService turnoService,
            ILogger<TurnosApiController> logger)
        {
            _context = context;
            _turnoService = turnoService;
            _logger = logger;
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
            
            turno.Estado = EstadoTurno.Aprobado;
            turno.RevisadoPorId = userId;
            turno.FechaRevision = DateTime.Now;
            turno.ComentariosFarmaceutico = model?.Comentarios;

            // Asignar fecha si no tiene
            if (!turno.FechaPreferida.HasValue)
            {
                turno.FechaPreferida = await _turnoService.GetNextAvailableSlotAsync();
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Turno {Id} aprobado vía API por usuario {UserId}", id, userId);

            return ApiOk(MapToDto(turno), "Turno aprobado exitosamente");
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

            turno.Estado = EstadoTurno.Rechazado;
            turno.RevisadoPorId = userId;
            turno.FechaRevision = DateTime.Now;
            turno.ComentariosFarmaceutico = model.Motivo;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Turno {Id} rechazado vía API por usuario {UserId}", id, userId);

            return ApiOk(MapToDto(turno), "Turno rechazado");
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
                FechaPreferida = t.FechaPreferida,
                FechaSolicitud = t.FechaSolicitud,
                Estado = t.Estado,
                NotasSolicitante = t.NotasSolicitante,
                ComentariosFarmaceutico = t.ComentariosFarmaceutico,
                FechaRevision = t.FechaRevision,
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
                DocumentosCount = t.Documentos?.Count ?? 0
            };
        }

        #endregion
    }
}
