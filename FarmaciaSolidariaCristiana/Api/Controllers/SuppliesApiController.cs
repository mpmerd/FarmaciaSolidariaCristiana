using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Api.Models;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// API para gestión de insumos médicos
    /// </summary>
    [Route("api/supplies")]
    public class SuppliesApiController : ApiBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuppliesApiController> _logger;

        public SuppliesApiController(
            ApplicationDbContext context,
            ILogger<SuppliesApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los insumos
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<List<SupplyDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            var query = _context.Supplies.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(s => s.Name.Contains(search) || 
                                        (s.Description != null && s.Description.Contains(search)));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var supplies = await query
                .OrderBy(s => s.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SupplyDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    StockQuantity = s.StockQuantity,
                    Unit = s.Unit
                })
                .ToListAsync();

            return ApiOk(new PagedResult<SupplyDto>
            {
                Items = supplies,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Obtiene un insumo por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<SupplyDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var supply = await _context.Supplies
                .Where(s => s.Id == id)
                .Select(s => new SupplyDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    StockQuantity = s.StockQuantity,
                    Unit = s.Unit
                })
                .FirstOrDefaultAsync();

            if (supply == null)
            {
                return ApiError("Insumo no encontrado", 404);
            }

            return ApiOk(supply);
        }

        /// <summary>
        /// Crea un nuevo insumo
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<SupplyDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateSupplyDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            var supply = new Supply
            {
                Name = model.Name,
                Description = model.Description,
                StockQuantity = model.StockQuantity,
                Unit = model.Unit ?? "Unidades"
            };

            _context.Supplies.Add(supply);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Insumo creado vía API: {Name} (ID: {Id})", supply.Name, supply.Id);

            var result = new SupplyDto
            {
                Id = supply.Id,
                Name = supply.Name,
                Description = supply.Description,
                StockQuantity = supply.StockQuantity,
                Unit = supply.Unit
            };

            return CreatedAtAction(nameof(GetById), new { id = supply.Id },
                new ApiResponse<SupplyDto>
                {
                    Success = true,
                    Message = "Insumo creado exitosamente",
                    Data = result
                });
        }

        /// <summary>
        /// Actualiza un insumo existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<SupplyDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateSupplyDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            var supply = await _context.Supplies.FindAsync(id);
            if (supply == null)
            {
                return ApiError("Insumo no encontrado", 404);
            }

            supply.Name = model.Name;
            supply.Description = model.Description;
            supply.StockQuantity = model.StockQuantity;
            supply.Unit = model.Unit ?? supply.Unit;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Insumo actualizado vía API: {Name} (ID: {Id})", supply.Name, supply.Id);

            return ApiOk(new SupplyDto
            {
                Id = supply.Id,
                Name = supply.Name,
                Description = supply.Description,
                StockQuantity = supply.StockQuantity,
                Unit = supply.Unit
            }, "Insumo actualizado exitosamente");
        }

        /// <summary>
        /// Elimina un insumo
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Delete(int id)
        {
            var supply = await _context.Supplies.FindAsync(id);
            if (supply == null)
            {
                return ApiError("Insumo no encontrado", 404);
            }

            // Verificar si tiene entregas o donaciones asociadas
            var hasDeliveries = await _context.Deliveries.AnyAsync(d => d.SupplyId == id);
            var hasDonations = await _context.Donations.AnyAsync(d => d.SupplyId == id);

            if (hasDeliveries || hasDonations)
            {
                return ApiError("No se puede eliminar el insumo porque tiene entregas o donaciones asociadas");
            }

            _context.Supplies.Remove(supply);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Insumo eliminado vía API: {Name} (ID: {Id})", supply.Name, id);

            return ApiOk(true, "Insumo eliminado exitosamente");
        }

        /// <summary>
        /// Obtiene insumos con stock disponible
        /// </summary>
        [HttpGet("available")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<List<SupplyDto>>), 200)]
        public async Task<IActionResult> GetAvailable()
        {
            var supplies = await _context.Supplies
                .Where(s => s.StockQuantity > 0)
                .OrderBy(s => s.Name)
                .Select(s => new SupplyDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    StockQuantity = s.StockQuantity,
                    Unit = s.Unit
                })
                .ToListAsync();

            return ApiOk(supplies);
        }
    }
}
