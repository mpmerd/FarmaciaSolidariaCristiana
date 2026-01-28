using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Api.Models;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// API para gestión de entregas
    /// </summary>
    [Route("api/deliveries")]
    public class DeliveriesApiController : ApiBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeliveriesApiController> _logger;

        public DeliveriesApiController(
            ApplicationDbContext context,
            ILogger<DeliveriesApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todas las entregas con filtros opcionales
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<DeliveryDto>>), 200)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20, 
            [FromQuery] string? search = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? type = null, // "medicine" o "supply"
            [FromQuery] int? turnoId = null)
        {
            var query = _context.Deliveries
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .Include(d => d.Patient)
                .Include(d => d.Turno)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(d => 
                    (d.Medicine != null && d.Medicine.Name.Contains(search)) ||
                    (d.Supply != null && d.Supply.Name.Contains(search)) ||
                    d.PatientIdentification.Contains(search) ||
                    (d.Patient != null && d.Patient.FullName.Contains(search)));
            }

            if (startDate.HasValue)
            {
                query = query.Where(d => d.DeliveryDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(d => d.DeliveryDate <= endDate.Value);
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

            if (turnoId.HasValue)
            {
                query = query.Where(d => d.TurnoId == turnoId.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var deliveries = await query
                .OrderByDescending(d => d.DeliveryDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(d => new DeliveryDto
                {
                    Id = d.Id,
                    PatientIdentification = d.PatientIdentification,
                    PatientId = d.PatientId,
                    PatientName = d.Patient != null ? d.Patient.FullName : null,
                    MedicineId = d.MedicineId,
                    MedicineName = d.Medicine != null ? d.Medicine.Name : null,
                    SupplyId = d.SupplyId,
                    SupplyName = d.Supply != null ? d.Supply.Name : null,
                    TurnoId = d.TurnoId,
                    Quantity = d.Quantity,
                    DeliveryDate = d.DeliveryDate,
                    CreatedAt = d.CreatedAt,
                    Dosage = d.Dosage,
                    TreatmentDuration = d.TreatmentDuration,
                    BatchNumber = d.BatchNumber,
                    ExpiryDate = d.ExpiryDate,
                    DeliveredBy = d.DeliveredBy,
                    Comments = d.Comments
                })
                .ToListAsync();

            return ApiOk(new PagedResult<DeliveryDto>
            {
                Items = deliveries,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Obtiene una entrega por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<DeliveryDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .Include(d => d.Patient)
                .Include(d => d.Turno)
                .Where(d => d.Id == id)
                .Select(d => new DeliveryDto
                {
                    Id = d.Id,
                    PatientIdentification = d.PatientIdentification,
                    PatientId = d.PatientId,
                    PatientName = d.Patient != null ? d.Patient.FullName : null,
                    MedicineId = d.MedicineId,
                    MedicineName = d.Medicine != null ? d.Medicine.Name : null,
                    SupplyId = d.SupplyId,
                    SupplyName = d.Supply != null ? d.Supply.Name : null,
                    TurnoId = d.TurnoId,
                    Quantity = d.Quantity,
                    DeliveryDate = d.DeliveryDate,
                    CreatedAt = d.CreatedAt,
                    Dosage = d.Dosage,
                    TreatmentDuration = d.TreatmentDuration,
                    BatchNumber = d.BatchNumber,
                    ExpiryDate = d.ExpiryDate,
                    DeliveredBy = d.DeliveredBy,
                    Comments = d.Comments
                })
                .FirstOrDefaultAsync();

            if (delivery == null)
            {
                return ApiError("Entrega no encontrada", 404);
            }

            return ApiOk(delivery);
        }

        /// <summary>
        /// Crea una nueva entrega (reduce stock automáticamente)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<DeliveryDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateDeliveryDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            // Validar que se proporcione medicamento O insumo
            if (!model.MedicineId.HasValue && !model.SupplyId.HasValue)
            {
                return ApiError("Debe especificar un medicamento o un insumo");
            }

            if (model.MedicineId.HasValue && model.SupplyId.HasValue)
            {
                return ApiError("Solo puede especificar medicamento O insumo, no ambos");
            }

            string itemName = "";

            // Verificar stock y actualizar
            if (model.MedicineId.HasValue)
            {
                var medicine = await _context.Medicines.FindAsync(model.MedicineId.Value);
                if (medicine == null)
                {
                    return ApiError("Medicamento no encontrado", 404);
                }
                if (medicine.StockQuantity < model.Quantity)
                {
                    return ApiError($"Stock insuficiente. Disponible: {medicine.StockQuantity} {medicine.Unit}");
                }
                medicine.StockQuantity -= model.Quantity;
                itemName = medicine.Name;
            }
            else if (model.SupplyId.HasValue)
            {
                var supply = await _context.Supplies.FindAsync(model.SupplyId.Value);
                if (supply == null)
                {
                    return ApiError("Insumo no encontrado", 404);
                }
                if (supply.StockQuantity < model.Quantity)
                {
                    return ApiError($"Stock insuficiente. Disponible: {supply.StockQuantity} {supply.Unit}");
                }
                supply.StockQuantity -= model.Quantity;
                itemName = supply.Name;
            }

            // Verificar paciente si se proporciona
            if (model.PatientId.HasValue)
            {
                var patient = await _context.Patients.FindAsync(model.PatientId.Value);
                if (patient == null)
                {
                    return ApiError("Paciente no encontrado", 404);
                }
            }

            // Verificar turno si se proporciona
            if (model.TurnoId.HasValue)
            {
                var turno = await _context.Turnos.FindAsync(model.TurnoId.Value);
                if (turno == null)
                {
                    return ApiError("Turno no encontrado", 404);
                }
            }

            var delivery = new Delivery
            {
                PatientIdentification = model.PatientIdentification,
                PatientId = model.PatientId,
                MedicineId = model.MedicineId,
                SupplyId = model.SupplyId,
                TurnoId = model.TurnoId,
                Quantity = model.Quantity,
                DeliveryDate = model.DeliveryDate ?? DateTime.Now,
                CreatedAt = DateTime.Now,
                Dosage = model.Dosage,
                TreatmentDuration = model.TreatmentDuration,
                BatchNumber = model.BatchNumber,
                ExpiryDate = model.ExpiryDate,
                DeliveredBy = model.DeliveredBy,
                PatientNote = model.PatientNote,
                Comments = model.Comments
            };

            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Entrega creada vía API: {Item} x{Quantity} a {Patient} (ID: {Id})", 
                itemName, model.Quantity, model.PatientIdentification, delivery.Id);

            var result = new DeliveryDto
            {
                Id = delivery.Id,
                PatientIdentification = delivery.PatientIdentification,
                PatientId = delivery.PatientId,
                MedicineId = delivery.MedicineId,
                SupplyId = delivery.SupplyId,
                TurnoId = delivery.TurnoId,
                Quantity = delivery.Quantity,
                DeliveryDate = delivery.DeliveryDate,
                CreatedAt = delivery.CreatedAt,
                Dosage = delivery.Dosage,
                TreatmentDuration = delivery.TreatmentDuration,
                BatchNumber = delivery.BatchNumber,
                ExpiryDate = delivery.ExpiryDate,
                DeliveredBy = delivery.DeliveredBy,
                Comments = delivery.Comments
            };

            return CreatedAtAction(nameof(GetById), new { id = delivery.Id },
                new ApiResponse<DeliveryDto>
                {
                    Success = true,
                    Message = $"Entrega de {itemName} registrada exitosamente. Stock actualizado.",
                    Data = result
                });
        }

        /// <summary>
        /// Obtiene entregas por identificación de paciente
        /// </summary>
        [HttpGet("by-patient/{identification}")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<List<DeliveryDto>>), 200)]
        public async Task<IActionResult> GetByPatientIdentification(string identification)
        {
            var deliveries = await _context.Deliveries
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .Where(d => d.PatientIdentification == identification)
                .OrderByDescending(d => d.DeliveryDate)
                .Take(50)
                .Select(d => new DeliveryDto
                {
                    Id = d.Id,
                    PatientIdentification = d.PatientIdentification,
                    MedicineId = d.MedicineId,
                    MedicineName = d.Medicine != null ? d.Medicine.Name : null,
                    SupplyId = d.SupplyId,
                    SupplyName = d.Supply != null ? d.Supply.Name : null,
                    Quantity = d.Quantity,
                    DeliveryDate = d.DeliveryDate,
                    CreatedAt = d.CreatedAt
                })
                .ToListAsync();

            return ApiOk(deliveries);
        }

        /// <summary>
        /// Obtiene estadísticas de entregas
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<DeliveryStatsDto>), 200)]
        public async Task<IActionResult> GetStats([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
        {
            var query = _context.Deliveries.AsQueryable();

            if (startDate.HasValue)
            {
                query = query.Where(d => d.DeliveryDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(d => d.DeliveryDate <= endDate.Value);
            }

            var stats = new DeliveryStatsDto
            {
                TotalDeliveries = await query.CountAsync(),
                TotalQuantity = await query.SumAsync(d => d.Quantity),
                MedicineDeliveries = await query.CountAsync(d => d.MedicineId != null),
                SupplyDeliveries = await query.CountAsync(d => d.SupplyId != null),
                UniquePatients = await query.Select(d => d.PatientIdentification).Distinct().CountAsync()
            };

            return ApiOk(stats);
        }
    }
}
