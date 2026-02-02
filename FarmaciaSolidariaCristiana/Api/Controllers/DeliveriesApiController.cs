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
        /// Crea una nueva entrega (maneja stock según si tiene turno aprobado o no)
        /// - Con turno aprobado: stock YA reservado, solo devuelve diferencia si es entrega parcial
        /// - Sin turno: descuenta stock directamente
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
            bool stockYaReservado = false;
            int cantidadAprobada = 0;

            // Verificar si hay turno aprobado asociado (stock ya reservado)
            FarmaciaSolidariaCristiana.Models.Turno? turnoAprobado = null;
            if (model.TurnoId.HasValue)
            {
                turnoAprobado = await _context.Turnos
                    .Include(t => t.Medicamentos)
                    .Include(t => t.Insumos)
                    .FirstOrDefaultAsync(t => t.Id == model.TurnoId.Value);
                
                if (turnoAprobado == null)
                {
                    return ApiError("Turno no encontrado", 404);
                }
                
                if (turnoAprobado.Estado == "Aprobado")
                {
                    stockYaReservado = true;
                    
                    // Obtener cantidad aprobada del turno
                    if (model.MedicineId.HasValue)
                    {
                        var turnoMed = turnoAprobado.Medicamentos
                            .FirstOrDefault(tm => tm.MedicineId == model.MedicineId.Value);
                        cantidadAprobada = turnoMed?.CantidadAprobada ?? turnoMed?.CantidadSolicitada ?? 0;
                    }
                    else if (model.SupplyId.HasValue)
                    {
                        var turnoIns = turnoAprobado.Insumos
                            .FirstOrDefault(ti => ti.SupplyId == model.SupplyId.Value);
                        cantidadAprobada = turnoIns?.CantidadAprobada ?? turnoIns?.CantidadSolicitada ?? 0;
                    }
                    
                    // Validar que no se entregue más de lo aprobado
                    if (model.Quantity > cantidadAprobada)
                    {
                        return ApiError($"No se puede entregar más de lo aprobado ({cantidadAprobada} unidades)");
                    }
                }
            }

            // Manejar stock según el caso
            if (model.MedicineId.HasValue)
            {
                var medicine = await _context.Medicines.FindAsync(model.MedicineId.Value);
                if (medicine == null)
                {
                    return ApiError("Medicamento no encontrado", 404);
                }
                itemName = medicine.Name;

                if (!stockYaReservado)
                {
                    // Sin turno: descontar stock
                    if (medicine.StockQuantity < model.Quantity)
                    {
                        return ApiError($"Stock insuficiente. Disponible: {medicine.StockQuantity} {medicine.Unit}");
                    }
                    medicine.StockQuantity -= model.Quantity;
                    _logger.LogInformation("Stock descontado (sin turno) - {Medicine}: -{Qty}", medicine.Name, model.Quantity);
                }
                else if (model.Quantity < cantidadAprobada)
                {
                    // Entrega parcial: devolver diferencia al stock
                    int diferencia = cantidadAprobada - model.Quantity;
                    medicine.StockQuantity += diferencia;
                    _logger.LogInformation("Entrega parcial - {Medicine}: devueltas {Diff} unidades al stock", medicine.Name, diferencia);
                }
            }
            else if (model.SupplyId.HasValue)
            {
                var supply = await _context.Supplies.FindAsync(model.SupplyId.Value);
                if (supply == null)
                {
                    return ApiError("Insumo no encontrado", 404);
                }
                itemName = supply.Name;

                if (!stockYaReservado)
                {
                    // Sin turno: descontar stock
                    if (supply.StockQuantity < model.Quantity)
                    {
                        return ApiError($"Stock insuficiente. Disponible: {supply.StockQuantity} {supply.Unit}");
                    }
                    supply.StockQuantity -= model.Quantity;
                    _logger.LogInformation("Stock descontado (sin turno) - {Supply}: -{Qty}", supply.Name, model.Quantity);
                }
                else if (model.Quantity < cantidadAprobada)
                {
                    // Entrega parcial: devolver diferencia al stock
                    int diferencia = cantidadAprobada - model.Quantity;
                    supply.StockQuantity += diferencia;
                    _logger.LogInformation("Entrega parcial - {Supply}: devueltas {Diff} unidades al stock", supply.Name, diferencia);
                }
            }

            // Buscar o crear relación con paciente
            int? patientId = model.PatientId;
            if (!patientId.HasValue && !string.IsNullOrEmpty(model.PatientIdentification))
            {
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.IdentificationDocument == model.PatientIdentification.Trim().ToUpper());
                patientId = patient?.Id;
            }

            var deliveredBy = model.DeliveredBy ?? User.Identity?.Name ?? "API";

            var delivery = new Delivery
            {
                PatientIdentification = model.PatientIdentification.Trim().ToUpper(),
                PatientId = patientId,
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
                DeliveredBy = deliveredBy,
                PatientNote = model.PatientNote,
                Comments = model.Comments
            };

            _context.Deliveries.Add(delivery);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Entrega creada vía API: {Item} x{Quantity} a {Patient} (ID: {Id}, TurnoId: {TurnoId})", 
                itemName, model.Quantity, model.PatientIdentification, delivery.Id, model.TurnoId?.ToString() ?? "NULL");

            // Marcar turno como Completado si existe
            if (model.TurnoId.HasValue && turnoAprobado != null)
            {
                turnoAprobado.Estado = "Completado";
                turnoAprobado.FechaEntrega = DateTime.Now;
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Turno #{TurnoId} marcado como Completado", model.TurnoId);
            }
            else
            {
                // Intentar completar turno automáticamente si existe uno aprobado para este paciente/producto
                await CompleteTurnoIfExistsAsync(model.PatientIdentification, model.MedicineId, model.SupplyId);
            }

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

        /// <summary>
        /// Marca un turno aprobado como completado si existe para el paciente y producto
        /// </summary>
        private async Task CompleteTurnoIfExistsAsync(string documentoIdentidad, int? medicineId, int? supplyId)
        {
            try
            {
                // Normalizar documento (igual que TurnoService.HashDocument)
                documentoIdentidad = documentoIdentidad.Trim().ToUpper();
                
                // Calcular hash del documento
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(documentoIdentidad));
                var documentHash = Convert.ToBase64String(hashBytes);

                // Buscar turnos aprobados para este paciente
                var turnos = await _context.Turnos
                    .Include(t => t.Medicamentos)
                    .Include(t => t.Insumos)
                    .Where(t => 
                        t.DocumentoIdentidadHash == documentHash && 
                        t.Estado == "Aprobado")
                    .ToListAsync();

                if (!turnos.Any())
                {
                    _logger.LogDebug("API: No se encontró turno aprobado para documento {Hash}", documentHash);
                    return;
                }

                // Buscar turno que coincida con el producto entregado
                FarmaciaSolidariaCristiana.Models.Turno? turnoCoincidente = null;

                if (medicineId.HasValue)
                {
                    turnoCoincidente = turnos.FirstOrDefault(t => 
                        t.Medicamentos.Any(m => m.MedicineId == medicineId.Value));
                }
                else if (supplyId.HasValue)
                {
                    turnoCoincidente = turnos.FirstOrDefault(t => 
                        t.Insumos.Any(i => i.SupplyId == supplyId.Value));
                }

                if (turnoCoincidente != null)
                {
                    turnoCoincidente.Estado = "Completado";
                    turnoCoincidente.FechaEntrega = DateTime.Now;
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("API: ✅ Turno #{TurnoId} marcado como Completado automáticamente", turnoCoincidente.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "API: Error al intentar completar turno automáticamente");
                // No lanzar excepción - la entrega ya se creó exitosamente
            }
        }
    }
}
