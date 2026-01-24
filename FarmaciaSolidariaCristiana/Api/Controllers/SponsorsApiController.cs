using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Api.Models;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// API para gestión de patrocinadores
    /// </summary>
    [Route("api/sponsors")]
    public class SponsorsApiController : ApiBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SponsorsApiController> _logger;

        public SponsorsApiController(
            ApplicationDbContext context,
            ILogger<SponsorsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los patrocinadores activos (público)
        /// </summary>
        [HttpGet]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<List<SponsorDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            var query = _context.Sponsors.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(s => s.IsActive);
            }

            var sponsors = await query
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .Select(s => new SponsorDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    LogoPath = s.LogoPath,
                    IsActive = s.IsActive,
                    DisplayOrder = s.DisplayOrder,
                    CreatedDate = s.CreatedDate
                })
                .ToListAsync();

            return ApiOk(sponsors);
        }

        /// <summary>
        /// Obtiene un patrocinador por ID
        /// </summary>
        [HttpGet("{id}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<SponsorDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var sponsor = await _context.Sponsors
                .Where(s => s.Id == id)
                .Select(s => new SponsorDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    LogoPath = s.LogoPath,
                    IsActive = s.IsActive,
                    DisplayOrder = s.DisplayOrder,
                    CreatedDate = s.CreatedDate
                })
                .FirstOrDefaultAsync();

            if (sponsor == null)
            {
                return ApiError("Patrocinador no encontrado", 404);
            }

            return ApiOk(sponsor);
        }

        /// <summary>
        /// Crea un nuevo patrocinador (sin logo, para logo usar endpoint web)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<SponsorDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateSponsorDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            var sponsor = new Sponsor
            {
                Name = model.Name,
                Description = model.Description,
                IsActive = model.IsActive,
                DisplayOrder = model.DisplayOrder,
                CreatedDate = DateTime.Now
            };

            _context.Sponsors.Add(sponsor);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Patrocinador creado vía API: {Name} (ID: {Id})", sponsor.Name, sponsor.Id);

            var result = new SponsorDto
            {
                Id = sponsor.Id,
                Name = sponsor.Name,
                Description = sponsor.Description,
                IsActive = sponsor.IsActive,
                DisplayOrder = sponsor.DisplayOrder,
                CreatedDate = sponsor.CreatedDate
            };

            return CreatedAtAction(nameof(GetById), new { id = sponsor.Id },
                new ApiResponse<SponsorDto>
                {
                    Success = true,
                    Message = "Patrocinador creado exitosamente",
                    Data = result
                });
        }

        /// <summary>
        /// Actualiza un patrocinador existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<SponsorDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSponsorDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor == null)
            {
                return ApiError("Patrocinador no encontrado", 404);
            }

            sponsor.Name = model.Name;
            sponsor.Description = model.Description;
            sponsor.IsActive = model.IsActive;
            sponsor.DisplayOrder = model.DisplayOrder;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Patrocinador actualizado vía API: {Name} (ID: {Id})", sponsor.Name, sponsor.Id);

            return ApiOk(new SponsorDto
            {
                Id = sponsor.Id,
                Name = sponsor.Name,
                Description = sponsor.Description,
                LogoPath = sponsor.LogoPath,
                IsActive = sponsor.IsActive,
                DisplayOrder = sponsor.DisplayOrder,
                CreatedDate = sponsor.CreatedDate
            }, "Patrocinador actualizado exitosamente");
        }

        /// <summary>
        /// Elimina un patrocinador
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Delete(int id)
        {
            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor == null)
            {
                return ApiError("Patrocinador no encontrado", 404);
            }

            _context.Sponsors.Remove(sponsor);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Patrocinador eliminado vía API: {Name} (ID: {Id})", sponsor.Name, id);

            return ApiOk(true, "Patrocinador eliminado exitosamente");
        }

        /// <summary>
        /// Activa o desactiva un patrocinador
        /// </summary>
        [HttpPost("{id}/toggle-active")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor == null)
            {
                return ApiError("Patrocinador no encontrado", 404);
            }

            sponsor.IsActive = !sponsor.IsActive;
            await _context.SaveChangesAsync();

            var status = sponsor.IsActive ? "activado" : "desactivado";
            _logger.LogInformation("Patrocinador {Status} vía API: {Name} (ID: {Id})", status, sponsor.Name, id);

            return ApiOk(sponsor.IsActive, $"Patrocinador {status} exitosamente");
        }
    }
}
