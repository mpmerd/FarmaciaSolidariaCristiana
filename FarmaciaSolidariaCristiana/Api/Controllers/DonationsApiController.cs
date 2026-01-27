using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Api.Models;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// API para gestión de donaciones
    /// </summary>
    [Route("api/donations")]
    public class DonationsApiController : ApiBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DonationsApiController> _logger;

        public DonationsApiController(
            ApplicationDbContext context,
            ILogger<DonationsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las donaciones con filtros opcionales
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<DonationDto>>), 200)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20, 
            [FromQuery] string? search = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? type = null) // "medicine" o "supply"
        {
            var query = _context.Donations
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d => 
                    (d.Medicine != null && d.Medicine.Name.Contains(search)) ||
                    (d.Supply != null && d.Supply.Name.Contains(search)) ||
                    (d.DonorNote != null && d.DonorNote.Contains(search)));
            }

            if (startDate.HasValue)
            {
                query = query.Where(d => d.DonationDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(d => d.DonationDate <= endDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                if (type.Equals("medicine", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(d => d.MedicineId != null);
                }
                else if (type.Equals("supply", StringComparison.OrdinalIgnoreCase))
                {
                    query = query.Where(d => d.SupplyId != null);
                }
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var donations = await query
                .OrderByDescending(d => d.DonationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DonationDto
                {
                    Id = d.Id,
                    MedicineId = d.MedicineId,
                    MedicineName = d.Medicine != null ? d.Medicine.Name : null,
                    SupplyId = d.SupplyId,
                    SupplyName = d.Supply != null ? d.Supply.Name : null,
                    Quantity = d.Quantity,
                    DonationDate = d.DonationDate,
                    DonorNote = d.DonorNote,
                    Comments = d.Comments
                })
                .ToListAsync();

            return ApiOk(new PagedResult<DonationDto>
            {
                Items = donations,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Obtiene una donación por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<DonationDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var donation = await _context.Donations
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .Where(d => d.Id == id)
                .Select(d => new DonationDto
                {
                    Id = d.Id,
                    MedicineId = d.MedicineId,
                    MedicineName = d.Medicine != null ? d.Medicine.Name : null,
                    SupplyId = d.SupplyId,
                    SupplyName = d.Supply != null ? d.Supply.Name : null,
                    Quantity = d.Quantity,
                    DonationDate = d.DonationDate,
                    DonorNote = d.DonorNote,
                    Comments = d.Comments
                })
                .FirstOrDefaultAsync();

            if (donation == null)
            {
                return ApiError("Donación no encontrada", 404);
            }

            return ApiOk(donation);
        }

        /// <summary>
        /// Crea una nueva donación (aumenta stock automáticamente)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<DonationDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateDonationDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            // Validar que se proporcione medicamento O insumo (pero no ambos ni ninguno)
            if (!model.MedicineId.HasValue && !model.SupplyId.HasValue)
            {
                return ApiError("Debe especificar un medicamento o un insumo");
            }

            if (model.MedicineId.HasValue && model.SupplyId.HasValue)
            {
                return ApiError("Solo puede especificar medicamento O insumo, no ambos");
            }

            string itemName = "";

            // Actualizar stock
            if (model.MedicineId.HasValue)
            {
                var medicine = await _context.Medicines.FindAsync(model.MedicineId.Value);
                if (medicine == null)
                {
                    return ApiError("Medicamento no encontrado", 404);
                }
                medicine.StockQuantity += model.Quantity;
                itemName = medicine.Name;
            }
            else if (model.SupplyId.HasValue)
            {
                var supply = await _context.Supplies.FindAsync(model.SupplyId.Value);
                if (supply == null)
                {
                    return ApiError("Insumo no encontrado", 404);
                }
                supply.StockQuantity += model.Quantity;
                itemName = supply.Name;
            }

            var donation = new Donation
            {
                MedicineId = model.MedicineId,
                SupplyId = model.SupplyId,
                Quantity = model.Quantity,
                DonationDate = model.DonationDate ?? DateTime.Now,
                DonorNote = model.DonorNote,
                Comments = model.Comments
            };

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Donación creada vía API: {Item} x{Quantity} (ID: {Id})", itemName, model.Quantity, donation.Id);

            var result = new DonationDto
            {
                Id = donation.Id,
                MedicineId = donation.MedicineId,
                SupplyId = donation.SupplyId,
                Quantity = donation.Quantity,
                DonationDate = donation.DonationDate,
                DonorNote = donation.DonorNote,
                Comments = donation.Comments
            };

            return CreatedAtAction(nameof(GetById), new { id = donation.Id },
                new ApiResponse<DonationDto>
                {
                    Success = true,
                    Message = $"Donación de {itemName} registrada exitosamente. Stock actualizado.",
                    Data = result
                });
        }

        /// <summary>
        /// Obtiene estadísticas de donaciones
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<DonationStatsDto>), 200)]
        public async Task<IActionResult> GetStats([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var query = _context.Donations.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(d => d.DonationDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(d => d.DonationDate <= endDate.Value);
            }

            var stats = new DonationStatsDto
            {
                TotalDonations = await query.CountAsync(),
                TotalQuantity = await query.SumAsync(d => d.Quantity),
                MedicineDonations = await query.CountAsync(d => d.MedicineId != null),
                SupplyDonations = await query.CountAsync(d => d.SupplyId != null)
            };

            return ApiOk(stats);
        }
    }
}
