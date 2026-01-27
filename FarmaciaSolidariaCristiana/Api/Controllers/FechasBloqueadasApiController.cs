using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using System.Security.Claims;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// API para gestión de fechas bloqueadas
    /// </summary>
    [Route("api/fechasbloqueadas")]
    public class FechasBloqueadasApiController : ApiBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FechasBloqueadasApiController> _logger;

        public FechasBloqueadasApiController(
            ApplicationDbContext context,
            ILogger<FechasBloqueadasApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las fechas bloqueadas
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<List<FechaBloqueadaDto>>), 200)]
        public async Task<IActionResult> GetAll()
        {
            var fechas = await _context.FechasBloqueadas
                .OrderByDescending(f => f.Fecha)
                .Select(f => new FechaBloqueadaDto
                {
                    Id = f.Id,
                    Fecha = f.Fecha,
                    Motivo = f.Motivo,
                    CreatedAt = f.FechaCreacion
                })
                .ToListAsync();

            return ApiOk(fechas);
        }

        /// <summary>
        /// Crea una nueva fecha bloqueada
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<FechaBloqueadaDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateFechaBloqueadaRequest request)
        {
            if (!DateTime.TryParse(request.Fecha, out var fecha))
            {
                return ApiError("Fecha inválida");
            }

            if (fecha.Date <= DateTime.Today)
            {
                return ApiError("La fecha debe ser posterior a hoy");
            }

            // Verificar si ya existe
            var exists = await _context.FechasBloqueadas
                .AnyAsync(f => f.Fecha.Date == fecha.Date);

            if (exists)
            {
                return ApiError("Ya existe un bloqueo para esta fecha");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";

            var fechaBloqueada = new FechaBloqueada
            {
                Fecha = fecha.Date,
                Motivo = request.Motivo ?? "Fecha bloqueada",
                UsuarioId = userId,
                FechaCreacion = DateTime.Now
            };

            _context.FechasBloqueadas.Add(fechaBloqueada);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Fecha bloqueada creada: {Fecha} por usuario {UserId}", fecha.Date, userId);

            return ApiOk(new FechaBloqueadaDto
            {
                Id = fechaBloqueada.Id,
                Fecha = fechaBloqueada.Fecha,
                Motivo = fechaBloqueada.Motivo,
                CreatedAt = fechaBloqueada.FechaCreacion
            });
        }

        /// <summary>
        /// Elimina una fecha bloqueada
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Delete(int id)
        {
            var fechaBloqueada = await _context.FechasBloqueadas.FindAsync(id);
            
            if (fechaBloqueada == null)
            {
                return ApiError("Fecha bloqueada no encontrada", 404);
            }

            _context.FechasBloqueadas.Remove(fechaBloqueada);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Fecha bloqueada eliminada: {Fecha}", fechaBloqueada.Fecha);

            return ApiOk(true);
        }
    }

    /// <summary>
    /// DTO para crear una fecha bloqueada
    /// </summary>
    public class CreateFechaBloqueadaRequest
    {
        public string Fecha { get; set; } = string.Empty;
        public string? Motivo { get; set; }
    }

    /// <summary>
    /// DTO de respuesta de fecha bloqueada
    /// </summary>
    public class FechaBloqueadaDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public string? Motivo { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
