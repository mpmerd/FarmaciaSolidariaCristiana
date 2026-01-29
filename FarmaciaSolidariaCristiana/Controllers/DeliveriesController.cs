using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Controllers
{
    [Authorize]
    public class DeliveriesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DeliveriesController> _logger;

        public DeliveriesController(ApplicationDbContext context, ILogger<DeliveriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, DateTime? startDate, DateTime? endDate)
        {
            var deliveries = _context.Deliveries
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .Include(d => d.Turno) // ✅ Incluir Turno
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                deliveries = deliveries.Where(d => 
                    (d.Medicine != null && d.Medicine.Name.Contains(searchString)) ||
                    (d.Supply != null && d.Supply.Name.Contains(searchString)));
            }

            if (startDate.HasValue)
            {
                deliveries = deliveries.Where(d => d.DeliveryDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                deliveries = deliveries.Where(d => d.DeliveryDate <= endDate.Value);
            }

            ViewData["TotalDeliveries"] = await deliveries.SumAsync(d => d.Quantity);
            return View(await deliveries
                .OrderByDescending(d => d.DeliveryDate)
                .ToListAsync());
        }

        [Authorize(Roles = "Admin,Farmaceutico")]
        public IActionResult Create()
        {
            ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name");
            ViewData["SupplyId"] = new SelectList(_context.Supplies.OrderBy(s => s.Name), "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            string PatientIdentification,
            DateTime DeliveryDate,
            string? Comments,
            string? MedicineIds,
            string? MedicineQuantities,
            string? SupplyIds,
            string? SupplyQuantities)
        {
            // Validar que se hayan seleccionado medicamentos O insumos (pero no ambos ni ninguno)
            bool hasMedicines = !string.IsNullOrWhiteSpace(MedicineIds);
            bool hasSupplies = !string.IsNullOrWhiteSpace(SupplyIds);

            if (!hasMedicines && !hasSupplies)
            {
                TempData["ErrorMessage"] = "Debe seleccionar al menos un medicamento o un insumo.";
                return RedirectToAction(nameof(Create));
            }

            if (hasMedicines && hasSupplies)
            {
                TempData["ErrorMessage"] = "Solo puede seleccionar medicamentos O insumos, no ambos en la misma entrega.";
                return RedirectToAction(nameof(Create));
            }

            // Validar fecha de entrega
            var today = DateTime.Today;
            var deliveryDateOnly = DeliveryDate.Date;
            var minAllowedDate = today.AddDays(-5);

            if (deliveryDateOnly > today)
            {
                TempData["ErrorMessage"] = "La fecha de entrega no puede ser futura.";
                return RedirectToAction(nameof(Create));
            }

            if (deliveryDateOnly < minAllowedDate)
            {
                TempData["ErrorMessage"] = "La fecha de entrega no puede ser mayor a 5 días en el pasado.";
                return RedirectToAction(nameof(Create));
            }

            // Buscar el paciente
            var patient = await _context.Patients
                .FirstOrDefaultAsync(p => p.IdentificationDocument == PatientIdentification && p.IsActive);

            if (patient == null)
            {
                TempData["ErrorMessage"] = "Paciente no encontrado. Por favor, registre primero al paciente.";
                return RedirectToAction(nameof(Create));
            }

            // ⚠️ IMPORTANTE: Usar transacción para garantizar atomicidad y evitar race conditions en stock
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var deliveredBy = User.Identity?.Name ?? "Sistema";
                var createdAt = DateTime.Now;
                int deliveriesCount = 0;

                // ✅ NUEVO: Determinar el TurnoId y su estado ANTES de crear las entregas
                // El estado es importante porque:
                // - Aprobado: el stock YA está reservado, no descontar
                // - Pendiente: el stock NO está reservado, SÍ descontar
                int? turnoId = null;
                bool stockYaReservado = false; // true solo si turno está Aprobado
                if (hasMedicines)
                {
                    var firstMedicineId = MedicineIds!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).First();
                    var turnoInfo = await FindTurnoIdWithStateAsync(PatientIdentification, firstMedicineId, null);
                    turnoId = turnoInfo.turnoId;
                    stockYaReservado = turnoInfo.stockReservado;
                    _logger.LogInformation("🔍 Buscando turno para paciente {PatientId} con medicamento {MedicineId}. TurnoId: {TurnoId}, StockReservado: {StockReservado}",
                        PatientIdentification, firstMedicineId, turnoId?.ToString() ?? "NULL", stockYaReservado);
                }
                else if (hasSupplies)
                {
                    var firstSupplyId = SupplyIds!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).First();
                    var turnoInfo = await FindTurnoIdWithStateAsync(PatientIdentification, null, firstSupplyId);
                    turnoId = turnoInfo.turnoId;
                    stockYaReservado = turnoInfo.stockReservado;
                    _logger.LogInformation("🔍 Buscando turno para paciente {PatientId} con insumo {SupplyId}. TurnoId: {TurnoId}, StockReservado: {StockReservado}",
                        PatientIdentification, firstSupplyId, turnoId?.ToString() ?? "NULL", stockYaReservado);
                }

                if (hasMedicines)
                {
                    var medicineIdsList = MedicineIds!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                    var medicineQuantitiesList = MedicineQuantities!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

                    if (medicineIdsList.Count != medicineQuantitiesList.Count)
                    {
                        TempData["ErrorMessage"] = "Error en los datos de medicamentos. Por favor, inténtelo de nuevo.";
                        return RedirectToAction(nameof(Create));
                    }

                    for (int i = 0; i < medicineIdsList.Count; i++)
                    {
                        // ⚠️ Bloquear fila para evitar race conditions (SQL Server: UPDLOCK + ROWLOCK)
                        // Solo bloquear si NO es de un turno (turno ya reservó stock con lock)
                        Medicine? medicine;
                        if (turnoId == null)
                        {
                            medicine = await _context.Medicines
                                .FromSqlRaw("SELECT * FROM Medicines WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", medicineIdsList[i])
                                .FirstOrDefaultAsync();
                        }
                        else
                        {
                            medicine = await _context.Medicines.FindAsync(medicineIdsList[i]);
                        }

                        if (medicine == null)
                        {
                            await transaction.RollbackAsync();
                            TempData["ErrorMessage"] = $"Medicamento con ID {medicineIdsList[i]} no encontrado.";
                            return RedirectToAction(nameof(Create));
                        }

                        // ✅ Validar stock solo si NO es de turno (turno ya validó al aprobar)
                        if (turnoId == null && medicine.StockQuantity < medicineQuantitiesList[i])
                        {
                            await transaction.RollbackAsync();
                            _logger.LogWarning("⚠️ Stock insuficiente para {MedicineName}. Disponible: {Available}, Solicitado: {Requested}, TurnoId: {TurnoId}",
                                medicine.Name, medicine.StockQuantity, medicineQuantitiesList[i], "NULL");
                            TempData["ErrorMessage"] = $"Stock insuficiente para {medicine.Name}. Disponible: {medicine.StockQuantity} {medicine.Unit}";
                            return RedirectToAction(nameof(Create));
                        }
                        
                        // ✅ VALIDAR: Si es de turno, NO permitir entregar MÁS de lo aprobado
                        if (stockYaReservado && turnoId.HasValue)
                        {
                            var turnoValidacion = await _context.Turnos
                                .Include(t => t.Medicamentos)
                                .FirstOrDefaultAsync(t => t.Id == turnoId.Value);
                            
                            if (turnoValidacion != null)
                            {
                                var turnoMed = turnoValidacion.Medicamentos
                                    .FirstOrDefault(tm => tm.MedicineId == medicineIdsList[i]);
                                
                                if (turnoMed?.CantidadAprobada.HasValue == true)
                                {
                                    int cantidadAprobada = turnoMed.CantidadAprobada.Value;
                                    int cantidadAEntregar = medicineQuantitiesList[i];
                                    
                                    if (cantidadAEntregar > cantidadAprobada)
                                    {
                                        await transaction.RollbackAsync();
                                        _logger.LogWarning(
                                            "❌ Intento de entregar MÁS de lo aprobado - Medicine: {MedicineName}, " +
                                            "Aprobado: {Aprobado}, Solicitado: {Solicitado}",
                                            medicine.Name, cantidadAprobada, cantidadAEntregar);
                                        TempData["ErrorMessage"] = $"No se puede entregar {cantidadAEntregar} unidades de {medicine.Name}. " +
                                            $"El turno solo tiene aprobadas {cantidadAprobada} unidades. " +
                                            $"Por favor, corrija la cantidad.";
                                        return RedirectToAction(nameof(Create));
                                    }
                                }
                            }
                            
                            _logger.LogInformation("✅ Validación de turno OK para {MedicineName} (turno #{TurnoId}). Stock actual: {Stock}",
                                medicine.Name, turnoId, medicine.StockQuantity);
                        }

                        // Crear entrega
                        var delivery = new Delivery
                        {
                            PatientIdentification = PatientIdentification,
                            PatientId = patient.Id,
                            TurnoId = turnoId, // ✅ Asignar TurnoId
                            MedicineId = medicineIdsList[i],
                            Quantity = medicineQuantitiesList[i],
                            DeliveryDate = DeliveryDate,
                            Comments = Comments,
                            DeliveredBy = deliveredBy,
                            CreatedAt = createdAt
                        };

                        // ✅ CORREGIDO: Descontar stock si:
                        // - No hay turno asociado, O
                        // - Hay turno pero está en Pendiente (stock NO reservado)
                        if (!stockYaReservado)
                        {
                            medicine.StockQuantity -= medicineQuantitiesList[i];
                            string razon = turnoId == null ? "entrega sin turno" : $"turno #{turnoId} en Pendiente (stock no reservado)";
                            _logger.LogInformation("Stock descontado ({Razon}) - Medicine: {MedicineName}, Quantity: {Quantity}",
                                razon, medicine.Name, medicineQuantitiesList[i]);
                        }
                        else
                        {
                            // ✅ NUEVO: Si el turno tiene stock reservado, verificar si se entrega MENOS de lo aprobado
                            // En ese caso, devolver la diferencia al stock
                            var turno = await _context.Turnos
                                .Include(t => t.Medicamentos)
                                .FirstOrDefaultAsync(t => t.Id == turnoId);
                            
                            if (turno != null)
                            {
                                var turnoMed = turno.Medicamentos
                                    .FirstOrDefault(tm => tm.MedicineId == medicineIdsList[i]);
                                
                                if (turnoMed?.CantidadAprobada.HasValue == true)
                                {
                                    int cantidadAprobada = turnoMed.CantidadAprobada.Value;
                                    int cantidadEntregada = medicineQuantitiesList[i];
                                    
                                    if (cantidadEntregada < cantidadAprobada)
                                    {
                                        int diferencia = cantidadAprobada - cantidadEntregada;
                                        medicine.StockQuantity += diferencia;
                                        _logger.LogInformation(
                                            "⚠️ Entrega parcial - Medicine: {MedicineName}, Aprobado: {Aprobado}, Entregado: {Entregado}, " +
                                            "Diferencia devuelta al stock: +{Diferencia} (Stock resultante: {Stock})",
                                            medicine.Name, cantidadAprobada, cantidadEntregada, diferencia, medicine.StockQuantity);
                                    }
                                    else
                                    {
                                        _logger.LogInformation(
                                            "Stock YA reservado (turno #{TurnoId} Aprobado) - Medicine: {MedicineName}, Quantity: {Quantity}",
                                            turnoId, medicine.Name, medicineQuantitiesList[i]);
                                    }
                                }
                            }
                        }
                        
                        _context.Add(delivery);
                        deliveriesCount++;
                        
                        _logger.LogInformation("Delivery created for patient: {PatientName}, Medicine: {MedicineName}, Quantity: {Quantity}",
                            patient.FullName, medicine.Name, medicineQuantitiesList[i]);
                    }
                }
                else if (hasSupplies)
                {
                    var supplyIdsList = SupplyIds!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
                    var supplyQuantitiesList = SupplyQuantities!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();

                    if (supplyIdsList.Count != supplyQuantitiesList.Count)
                    {
                        TempData["ErrorMessage"] = "Error en los datos de insumos. Por favor, inténtelo de nuevo.";
                        return RedirectToAction(nameof(Create));
                    }

                    for (int i = 0; i < supplyIdsList.Count; i++)
                    {
                        // ⚠️ Bloquear fila para evitar race conditions (SQL Server: UPDLOCK + ROWLOCK)
                        // Solo bloquear si NO es de un turno (turno ya reservó stock con lock)
                        Supply? supply;
                        if (turnoId == null)
                        {
                            supply = await _context.Supplies
                                .FromSqlRaw("SELECT * FROM Supplies WITH (UPDLOCK, ROWLOCK) WHERE Id = {0}", supplyIdsList[i])
                                .FirstOrDefaultAsync();
                        }
                        else
                        {
                            supply = await _context.Supplies.FindAsync(supplyIdsList[i]);
                        }

                        if (supply == null)
                        {
                            await transaction.RollbackAsync();
                            TempData["ErrorMessage"] = $"Insumo con ID {supplyIdsList[i]} no encontrado.";
                            return RedirectToAction(nameof(Create));
                        }

                        // ✅ Validar stock solo si NO es de turno (turno ya validó al aprobar)
                        if (turnoId == null && supply.StockQuantity < supplyQuantitiesList[i])
                        {
                            await transaction.RollbackAsync();
                            TempData["ErrorMessage"] = $"Stock insuficiente para {supply.Name}. Disponible: {supply.StockQuantity} {supply.Unit}";
                            return RedirectToAction(nameof(Create));
                        }

                        // ✅ VALIDAR: Si es de turno, NO permitir entregar MÁS de lo aprobado
                        if (stockYaReservado && turnoId.HasValue)
                        {
                            var turnoValidacion = await _context.Turnos
                                .Include(t => t.Insumos)
                                .FirstOrDefaultAsync(t => t.Id == turnoId.Value);
                            
                            if (turnoValidacion != null)
                            {
                                var turnoIns = turnoValidacion.Insumos
                                    .FirstOrDefault(ti => ti.SupplyId == supplyIdsList[i]);
                                
                                if (turnoIns?.CantidadAprobada.HasValue == true)
                                {
                                    int cantidadAprobada = turnoIns.CantidadAprobada.Value;
                                    int cantidadAEntregar = supplyQuantitiesList[i];
                                    
                                    if (cantidadAEntregar > cantidadAprobada)
                                    {
                                        await transaction.RollbackAsync();
                                        _logger.LogWarning(
                                            "❌ Intento de entregar MÁS de lo aprobado - Supply: {SupplyName}, " +
                                            "Aprobado: {Aprobado}, Solicitado: {Solicitado}",
                                            supply.Name, cantidadAprobada, cantidadAEntregar);
                                        TempData["ErrorMessage"] = $"No se puede entregar {cantidadAEntregar} unidades de {supply.Name}. " +
                                            $"El turno solo tiene aprobadas {cantidadAprobada} unidades. " +
                                            $"Por favor, corrija la cantidad.";
                                        return RedirectToAction(nameof(Create));
                                    }
                                }
                            }
                        }

                        // Crear entrega
                        var delivery = new Delivery
                        {
                            PatientIdentification = PatientIdentification,
                            PatientId = patient.Id,
                            TurnoId = turnoId, // ✅ Asignar TurnoId
                            SupplyId = supplyIdsList[i],
                            Quantity = supplyQuantitiesList[i],
                            DeliveryDate = DeliveryDate,
                            Comments = Comments,
                            DeliveredBy = deliveredBy,
                            CreatedAt = createdAt
                        };

                        // ✅ CORREGIDO: Descontar stock si:
                        // - No hay turno asociado, O
                        // - Hay turno pero está en Pendiente (stock NO reservado)
                        if (!stockYaReservado)
                        {
                            supply.StockQuantity -= supplyQuantitiesList[i];
                            string razon = turnoId == null ? "entrega sin turno" : $"turno #{turnoId} en Pendiente (stock no reservado)";
                            _logger.LogInformation("Stock descontado ({Razon}) - Supply: {SupplyName}, Quantity: {Quantity}",
                                razon, supply.Name, supplyQuantitiesList[i]);
                        }
                        else
                        {
                            // ✅ NUEVO: Si el turno tiene stock reservado, verificar si se entrega MENOS de lo aprobado
                            // En ese caso, devolver la diferencia al stock
                            var turno = await _context.Turnos
                                .Include(t => t.Insumos)
                                .FirstOrDefaultAsync(t => t.Id == turnoId);
                            
                            if (turno != null)
                            {
                                var turnoIns = turno.Insumos
                                    .FirstOrDefault(ti => ti.SupplyId == supplyIdsList[i]);
                                
                                if (turnoIns?.CantidadAprobada.HasValue == true)
                                {
                                    int cantidadAprobada = turnoIns.CantidadAprobada.Value;
                                    int cantidadEntregada = supplyQuantitiesList[i];
                                    
                                    if (cantidadEntregada < cantidadAprobada)
                                    {
                                        int diferencia = cantidadAprobada - cantidadEntregada;
                                        supply.StockQuantity += diferencia;
                                        _logger.LogInformation(
                                            "⚠️ Entrega parcial - Supply: {SupplyName}, Aprobado: {Aprobado}, Entregado: {Entregado}, " +
                                            "Diferencia devuelta al stock: +{Diferencia} (Stock resultante: {Stock})",
                                            supply.Name, cantidadAprobada, cantidadEntregada, diferencia, supply.StockQuantity);
                                    }
                                    else
                                    {
                                        _logger.LogInformation(
                                            "Stock YA reservado (turno #{TurnoId} Aprobado) - Supply: {SupplyName}, Quantity: {Quantity}",
                                            turnoId, supply.Name, supplyQuantitiesList[i]);
                                    }
                                }
                            }
                        }
                        
                        _context.Add(delivery);
                        deliveriesCount++;
                        
                        _logger.LogInformation("Delivery created for patient: {PatientName}, Supply: {SupplyName}, Quantity: {Quantity}",
                            patient.FullName, supply.Name, supplyQuantitiesList[i]);
                    }
                }

                await _context.SaveChangesAsync();

                // ✅ Marcar turno como completado si corresponde (usar el primer item como referencia)
                if (hasMedicines)
                {
                    var firstMedicineId = MedicineIds!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).First();
                    await CompleteTurnoIfExistsAsync(PatientIdentification, firstMedicineId, null);
                }
                else if (hasSupplies)
                {
                    var firstSupplyId = SupplyIds!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).First();
                    await CompleteTurnoIfExistsAsync(PatientIdentification, null, firstSupplyId);
                }

                // ✅ Confirmar transacción - todos los cambios se aplicaron correctamente
                await transaction.CommitAsync();

                TempData["SuccessMessage"] = $"{deliveriesCount} entrega(s) registrada(s) exitosamente para {patient.FullName}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                // ⚠️ Rollback automático al salir del using, pero lo hacemos explícito para claridad
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error creating deliveries - Transacción revertida");
                TempData["ErrorMessage"] = "Error al registrar las entregas. Por favor, inténtelo de nuevo.";
                return RedirectToAction(nameof(Create));
            }
        }

        // GET: Deliveries/Delete/5
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var delivery = await _context.Deliveries
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .Include(d => d.Patient)
                .FirstOrDefaultAsync(m => m.Id == id);
                
            if (delivery == null)
            {
                return NotFound();
            }

            // Calcular el tiempo transcurrido desde la creación
            // Si CreatedAt es null (registros antiguos), usar DeliveryDate como referencia
            var createdDate = delivery.CreatedAt ?? delivery.DeliveryDate;
            var hoursSinceCreation = (DateTime.Now - createdDate).TotalHours;
            ViewData["HoursSinceCreation"] = hoursSinceCreation;
            ViewData["CanDelete"] = hoursSinceCreation <= 2;

            return View(delivery);
        }

        // POST: Deliveries/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var delivery = await _context.Deliveries
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .FirstOrDefaultAsync(d => d.Id == id);
                
            if (delivery == null)
            {
                return NotFound();
            }

            // Verificar que no hayan pasado más de 2 horas desde la creación
            // Si CreatedAt es null (registros antiguos), usar DeliveryDate como referencia
            var createdDate = delivery.CreatedAt ?? delivery.DeliveryDate;
            var hoursSinceCreation = (DateTime.Now - createdDate).TotalHours;
            
            if (hoursSinceCreation > 2)
            {
                TempData["ErrorMessage"] = "No se puede eliminar esta entrega porque han transcurrido más de 2 horas desde su creación.";
                _logger.LogWarning("Attempted to delete delivery after 2 hours: ID {Id}, Created: {CreatedAt}", 
                    delivery.Id, delivery.CreatedAt);
                return RedirectToAction(nameof(Index));
            }

            // Devolver al stock (medicamento o insumo)
            // SIEMPRE devolver stock al eliminar entrega:
            // - Si era de turno: devolvemos el stock reservado
            // - Si no era de turno: devolvemos el stock descontado al crear la entrega
            string itemName = "";
            if (delivery.Medicine != null)
            {
                delivery.Medicine.StockQuantity += delivery.Quantity;
                itemName = delivery.Medicine.Name;
                _logger.LogInformation("Stock devuelto - Medicine: {MedicineName}, Quantity: {Quantity}, TurnoId: {TurnoId}",
                    delivery.Medicine.Name, delivery.Quantity, delivery.TurnoId ?? 0);
            }
            else if (delivery.Supply != null)
            {
                delivery.Supply.StockQuantity += delivery.Quantity;
                itemName = delivery.Supply.Name;
                _logger.LogInformation("Stock devuelto - Supply: {SupplyName}, Quantity: {Quantity}, TurnoId: {TurnoId}",
                    delivery.Supply.Name, delivery.Quantity, delivery.TurnoId ?? 0);
            }

            _context.Deliveries.Remove(delivery);
            await _context.SaveChangesAsync();
            
            // ✅ NUEVO: Revertir turno correspondiente a Pendiente si fue completado
            await RevertTurnoIfNeededAsync(delivery);
            
            _logger.LogInformation("Delivery deleted: ID {Id}, Item: {Item}, Quantity: {Quantity}", 
                delivery.Id, itemName, delivery.Quantity);
            TempData["SuccessMessage"] = "Entrega eliminada exitosamente. El stock ha sido restaurado y el turno revertido a Pendiente.";

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Busca el ID del turno y determina si el stock ya está reservado
        /// Retorna (turnoId, stockReservado):
        /// - stockReservado = true si el turno está en "Aprobado" (stock ya descontado)
        /// - stockReservado = false si el turno está en "Pendiente" o no hay turno
        /// </summary>
        private async Task<(int? turnoId, bool stockReservado)> FindTurnoIdWithStateAsync(string documentoIdentidad, int? medicineId, int? supplyId)
        {
            try
            {
                // Normalizar documento (igual que TurnoService.HashDocument)
                documentoIdentidad = documentoIdentidad.Trim().ToUpper();
                
                // Calcular hash del documento
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(documentoIdentidad));
                var documentHash = Convert.ToBase64String(hashBytes);

                _logger.LogInformation("🔍 FindTurnoIdWithStateAsync - Documento: {Documento}, MedicineId: {MedicineId}, SupplyId: {SupplyId}",
                    documentoIdentidad, medicineId?.ToString() ?? "NULL", supplyId?.ToString() ?? "NULL");

                // ✅ SOLO buscar turnos APROBADOS (nunca Pendientes)
                // Los turnos Pendientes requieren aprobación de farmacéutico antes de poder entregar
                var turnos = await _context.Turnos
                    .Include(t => t.Medicamentos)
                    .Include(t => t.Insumos)
                    .Where(t => 
                        t.DocumentoIdentidadHash == documentHash && 
                        t.Estado == "Aprobado")
                    .OrderByDescending(t => t.FechaSolicitud)
                    .ToListAsync();

                if (!turnos.Any())
                {
                    _logger.LogInformation("❌ No se encontraron turnos para este documento");
                    return (null, false);
                }

                // Buscar el turno que contiene el item Y que no tenga ya una entrega activa
                foreach (var turno in turnos)
                {
                    if (medicineId.HasValue)
                    {
                        var turnoMedicamento = turno.Medicamentos
                            .FirstOrDefault(tm => tm.MedicineId == medicineId.Value);

                        // ✅ Solo si tiene CantidadAprobada (no fue eliminada/corregida)
                        if (turnoMedicamento != null && turnoMedicamento.CantidadAprobada.HasValue)
                        {
                            // Verificar si este medicamento específico ya tiene entrega activa
                            var yaEntregado = await _context.Deliveries
                                .AnyAsync(d => d.TurnoId == turno.Id && d.MedicineId == medicineId.Value);

                            if (!yaEntregado)
                            {
                                // Stock reservado porque el turno está Aprobado
                                _logger.LogInformation(
                                    "✅ Turno #{TurnoId} encontrado - Estado: Aprobado, StockReservado: true, MedicineId: {MedId}, CantidadAprobada: {Qty}",
                                    turno.Id, medicineId.Value, turnoMedicamento.CantidadAprobada);
                                return (turno.Id, true); // Stock siempre reservado para turnos Aprobados
                            }
                        }
                        else if (turnoMedicamento != null)
                        {
                            _logger.LogInformation(
                                "⚠️ Turno #{TurnoId} tiene el medicamento {MedId} pero sin CantidadAprobada (fue eliminado/corregido)",
                                turno.Id, medicineId.Value);
                        }
                    }
                    else if (supplyId.HasValue)
                    {
                        var turnoInsumo = turno.Insumos
                            .FirstOrDefault(ti => ti.SupplyId == supplyId.Value);

                        // ✅ Solo si tiene CantidadAprobada (no fue eliminada/corregida)
                        if (turnoInsumo != null && turnoInsumo.CantidadAprobada.HasValue)
                        {
                            // Verificar si este insumo específico ya tiene entrega activa
                            var yaEntregado = await _context.Deliveries
                                .AnyAsync(d => d.TurnoId == turno.Id && d.SupplyId == supplyId.Value);

                            if (!yaEntregado)
                            {
                                _logger.LogInformation(
                                    "✅ Turno #{TurnoId} encontrado - Estado: Aprobado, StockReservado: true, SupplyId: {SupId}, CantidadAprobada: {Qty}",
                                    turno.Id, supplyId.Value, turnoInsumo.CantidadAprobada);
                                return (turno.Id, true); // Stock siempre reservado para turnos Aprobados
                            }
                        }
                        else if (turnoInsumo != null)
                        {
                            _logger.LogInformation(
                                "⚠️ Turno #{TurnoId} tiene el insumo {SupId} pero sin CantidadAprobada (fue eliminado/corregido)",
                                turno.Id, supplyId.Value);
                        }
                    }
                }

                _logger.LogInformation("❌ No se encontró turno válido para el item especificado");
                return (null, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding turno ID with state");
                return (null, false);
            }
        }

        /// <summary>
        /// Busca el ID del turno que contiene el medicamento o insumo especificado
        /// SIN marcar como completado (solo buscar)
        /// </summary>
        private async Task<int?> FindTurnoIdAsync(string documentoIdentidad, int? medicineId, int? supplyId)
        {
            try
            {
                // Normalizar documento (igual que TurnoService.HashDocument)
                documentoIdentidad = documentoIdentidad.Trim().ToUpper();
                
                // Calcular hash del documento
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(documentoIdentidad));
                var documentHash = Convert.ToBase64String(hashBytes);

                _logger.LogInformation("🔍 FindTurnoIdAsync - Documento: {Documento}, Hash: {Hash}, MedicineId: {MedicineId}, SupplyId: {SupplyId}",
                    documentoIdentidad, documentHash, medicineId?.ToString() ?? "NULL", supplyId?.ToString() ?? "NULL");

                // ✅ SOLO buscar turnos APROBADOS (nunca Pendientes)
                // Los turnos Pendientes requieren aprobación de farmacéutico antes de poder entregar
                var turnos = await _context.Turnos
                    .Include(t => t.Medicamentos)
                    .Include(t => t.Insumos)
                    .Where(t => 
                        t.DocumentoIdentidadHash == documentHash && 
                        t.Estado == "Aprobado")
                    .OrderByDescending(t => t.FechaSolicitud)
                    .ToListAsync();

                _logger.LogInformation("🔍 Turnos encontrados: {Count}", turnos.Count);
                
                foreach (var t in turnos)
                {
                    _logger.LogInformation("🔍 Turno #{TurnoId} - Estado: {Estado}, Medicamentos: {MedCount}, Insumos: {InsCount}",
                        t.Id, t.Estado, t.Medicamentos.Count, t.Insumos.Count);
                }

                if (!turnos.Any())
                {
                    _logger.LogInformation("❌ No se encontraron turnos para este documento");
                    return null;
                }

                // Buscar el turno que contiene el item Y que no tenga ya una entrega activa para ese item
                // ✅ IMPORTANTE: También verificar que tenga CantidadAprobada
                foreach (var turno in turnos)
                {
                    if (medicineId.HasValue)
                    {
                        var turnoMedicamento = turno.Medicamentos
                            .FirstOrDefault(tm => tm.MedicineId == medicineId.Value);

                        // ✅ Solo si tiene CantidadAprobada (no fue eliminada/corregida)
                        if (turnoMedicamento != null && turnoMedicamento.CantidadAprobada.HasValue)
                        {
                            // Verificar si este medicamento específico ya tiene entrega activa
                            var yaEntregado = await _context.Deliveries
                                .AnyAsync(d => d.TurnoId == turno.Id && d.MedicineId == medicineId.Value);

                            if (!yaEntregado)
                            {
                                _logger.LogInformation(
                                    "✅ Turno #{TurnoId} encontrado para MedicineId {MedId} (cantidad aprobada: {Qty})",
                                    turno.Id, medicineId.Value, turnoMedicamento.CantidadAprobada);
                                return turno.Id;
                            }
                            else
                            {
                                _logger.LogInformation(
                                    "⚠️ Turno #{TurnoId} tiene el medicamento {MedId} pero ya fue entregado",
                                    turno.Id, medicineId.Value);
                            }
                        }
                        else if (turnoMedicamento != null)
                        {
                            _logger.LogInformation(
                                "⚠️ Turno #{TurnoId} tiene el medicamento {MedId} pero sin CantidadAprobada (fue eliminado/corregido)",
                                turno.Id, medicineId.Value);
                        }
                    }
                    else if (supplyId.HasValue)
                    {
                        var turnoInsumo = turno.Insumos
                            .FirstOrDefault(ti => ti.SupplyId == supplyId.Value);

                        // ✅ Solo si tiene CantidadAprobada (no fue eliminada/corregida)
                        if (turnoInsumo != null && turnoInsumo.CantidadAprobada.HasValue)
                        {
                            // Verificar si este insumo específico ya tiene entrega activa
                            var yaEntregado = await _context.Deliveries
                                .AnyAsync(d => d.TurnoId == turno.Id && d.SupplyId == supplyId.Value);

                            if (!yaEntregado)
                            {
                                _logger.LogInformation(
                                    "✅ Turno #{TurnoId} encontrado para SupplyId {SupId} (cantidad aprobada: {Qty})",
                                    turno.Id, supplyId.Value, turnoInsumo.CantidadAprobada);
                                return turno.Id;
                            }
                            else
                            {
                                _logger.LogInformation(
                                    "⚠️ Turno #{TurnoId} tiene el insumo {SupId} pero ya fue entregado",
                                    turno.Id, supplyId.Value);
                            }
                        }
                        else if (turnoInsumo != null)
                        {
                            _logger.LogInformation(
                                "⚠️ Turno #{TurnoId} tiene el insumo {SupId} pero sin CantidadAprobada (fue eliminado/corregido)",
                                turno.Id, supplyId.Value);
                        }
                    }
                }

                _logger.LogInformation("❌ No se encontró turno válido para el item especificado");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error finding turno ID");
                return null;
            }
        }

        /// <summary>
        /// Marca automáticamente un turno como completado si existe uno aprobado o pendiente para el documento dado
        /// Busca el turno específico que contiene el medicamento o insumo de la entrega
        /// También actualiza las cantidades aprobadas si no estaban establecidas
        /// RETORNA el ID del turno encontrado (o null si no hay turno)
        /// </summary>
        private async Task<int?> CompleteTurnoIfExistsAsync(string documentoIdentidad, int? medicineId, int? supplyId)
        {
            try
            {
                // Normalizar documento (igual que TurnoService.HashDocument)
                documentoIdentidad = documentoIdentidad.Trim().ToUpper();
                
                // Calcular hash del documento (mismo método que usa TurnoService)
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(documentoIdentidad));
                var documentHash = Convert.ToBase64String(hashBytes);

                // ✅ SOLO buscar turnos APROBADOS (nunca Pendientes)
                // Los turnos Pendientes requieren aprobación de farmacéutico antes de poder entregar
                var turnos = await _context.Turnos
                    .Include(t => t.Medicamentos)
                    .Include(t => t.Insumos)
                    .Where(t => 
                        t.DocumentoIdentidadHash == documentHash && 
                        t.Estado == "Aprobado")
                    .ToListAsync();

                if (!turnos.Any())
                {
                    return null; // No hay turnos aprobados para este paciente
                }

                // Buscar el turno ESPECÍFICO que contiene el item de la entrega
                // ✅ IMPORTANTE: También verificar que tenga CantidadAprobada
                Turno? turno = null;
                
                if (medicineId.HasValue)
                {
                    turno = turnos.FirstOrDefault(t => 
                        t.Medicamentos.Any(tm => tm.MedicineId == medicineId.Value && tm.CantidadAprobada.HasValue));
                }
                else if (supplyId.HasValue)
                {
                    turno = turnos.FirstOrDefault(t => 
                        t.Insumos.Any(ti => ti.SupplyId == supplyId.Value && ti.CantidadAprobada.HasValue));
                }

                if (turno != null)
                {
                    // ✅ NO establecer cantidades aprobadas automáticamente
                    // Las cantidades ya deben estar establecidas por el farmacéutico al aprobar
                    // Si no tienen CantidadAprobada, significa que fueron eliminadas/corregidas
                    
                    turno.Estado = "Completado";
                    turno.FechaEntrega = DateTime.Now;
                    await _context.SaveChangesAsync();
                    
                    string itemType = medicineId.HasValue ? "Medicamento" : "Insumo";
                    int itemId = medicineId ?? supplyId ?? 0;
                    _logger.LogInformation("Turno #{TurnoId} marcado como completado tras registrar entrega de {ItemType} ID {ItemId}", 
                        turno.Id, itemType, itemId);
                    
                    return turno.Id; // ✅ Retornar el ID del turno
                }
                
                return null; // No se encontró turno con este item
            }
            catch (Exception ex)
            {
                // Log error pero no fallar la entrega
                _logger.LogError(ex, "Error al intentar completar turno automáticamente para documento");
                return null;
            }
        }

        /// <summary>
        /// Revierte un turno completado a Pendiente cuando se elimina una entrega
        /// SOLO si no quedan más entregas asociadas a ese turno
        /// </summary>
        private async Task RevertTurnoIfNeededAsync(Delivery delivery)
        {
            try
            {
                // Calcular hash del documento
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(delivery.PatientIdentification));
                var documentHash = Convert.ToBase64String(hashBytes);

                // Buscar todos los turnos completados de este paciente
                var turnosCompletados = await _context.Turnos
                    .Include(t => t.Medicamentos)
                    .Include(t => t.Insumos)
                    .Where(t => t.DocumentoIdentidadHash == documentHash && t.Estado == "Completado")
                    .ToListAsync();

                if (!turnosCompletados.Any())
                {
                    return; // No hay turnos completados para revertir
                }

                // Buscar el turno específico que contiene el medicamento o insumo eliminado
                Turno? turnoARevertir = null;

                if (delivery.MedicineId.HasValue)
                {
                    // Buscar turno que tenga este medicamento
                    turnoARevertir = turnosCompletados
                        .FirstOrDefault(t => t.Medicamentos.Any(tm => tm.MedicineId == delivery.MedicineId.Value));
                }
                else if (delivery.SupplyId.HasValue)
                {
                    // Buscar turno que tenga este insumo
                    turnoARevertir = turnosCompletados
                        .FirstOrDefault(t => t.Insumos.Any(ti => ti.SupplyId == delivery.SupplyId.Value));
                }

                if (turnoARevertir != null)
                {
                    // ✅ LÓGICA MEJORADA: Verificar cuántas entregas quedan del turno
                    // Contar total de items en el turno
                    int totalItemsTurno = turnoARevertir.Medicamentos.Count + turnoARevertir.Insumos.Count;
                    
                    // Contar cuántas entregas quedan para los items de este turno
                    int entregasRestantes = 0;

                    // Contar entregas de medicamentos del turno
                    foreach (var tm in turnoARevertir.Medicamentos)
                    {
                        var entregas = await _context.Deliveries
                            .Where(d => 
                                d.TurnoId == turnoARevertir.Id &&
                                d.MedicineId == tm.MedicineId &&
                                d.Id != delivery.Id) // Excluir la entrega que se está eliminando
                            .CountAsync();
                        entregasRestantes += entregas;
                    }

                    // Contar entregas de insumos del turno
                    foreach (var ti in turnoARevertir.Insumos)
                    {
                        var entregas = await _context.Deliveries
                            .Where(d => 
                                d.TurnoId == turnoARevertir.Id &&
                                d.SupplyId == ti.SupplyId &&
                                d.Id != delivery.Id) // Excluir la entrega que se está eliminando
                            .CountAsync();
                        entregasRestantes += entregas;
                    }

                    _logger.LogInformation(
                        "Turno #{TurnoId}: Total items={Total}, Entregas restantes={Restantes}",
                        turnoARevertir.Id, totalItemsTurno, entregasRestantes);

                    if (entregasRestantes == 0)
                    {
                        // No quedan entregas - revertir a APROBADO (no Pendiente)
                        // El turno ya fue aprobado por un farmacéutico, esa es la realidad
                        turnoARevertir.Estado = "Aprobado";
                        turnoARevertir.FechaEntrega = null;
                        
                        // ✅ MANTENER las cantidades aprobadas originales
                        // NO limpiar CantidadAprobada - el turno sigue siendo válido
                        
                        // ✅ RE-RESERVAR el stock (ya fue devuelto al eliminar cada entrega)
                        // Esto mantiene la consistencia: turno Aprobado = stock reservado
                        foreach (var tm in turnoARevertir.Medicamentos)
                        {
                            if (tm.CantidadAprobada.HasValue)
                            {
                                var medicine = await _context.Medicines.FindAsync(tm.MedicineId);
                                if (medicine != null)
                                {
                                    medicine.StockQuantity -= tm.CantidadAprobada.Value;
                                    _logger.LogInformation(
                                        "🔄 Re-reservando stock de {MedicineName}: -{Qty} (Stock resultante: {Stock})",
                                        medicine.Name, tm.CantidadAprobada.Value, medicine.StockQuantity);
                                }
                            }
                        }
                        
                        foreach (var ti in turnoARevertir.Insumos)
                        {
                            if (ti.CantidadAprobada.HasValue)
                            {
                                var supply = await _context.Supplies.FindAsync(ti.SupplyId);
                                if (supply != null)
                                {
                                    supply.StockQuantity -= ti.CantidadAprobada.Value;
                                    _logger.LogInformation(
                                        "🔄 Re-reservando stock de {SupplyName}: -{Qty} (Stock resultante: {Stock})",
                                        supply.Name, ti.CantidadAprobada.Value, supply.StockQuantity);
                                }
                            }
                        }
                        
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation(
                            "✅ Turno #{TurnoId} revertido a APROBADO tras eliminar todas las entregas. Stock re-reservado.", 
                            turnoARevertir.Id);
                    }
                    else if (entregasRestantes < totalItemsTurno)
                    {
                        // ✅ Quedan algunas entregas pero no todas - revertir a Aprobado
                        turnoARevertir.Estado = "Aprobado";
                        turnoARevertir.FechaEntrega = null;
                        
                        // ✅ IMPORTANTE: Limpiar CantidadAprobada del item eliminado
                        // Esto asegura que:
                        // 1. El stock devuelto no quede "fantasma"
                        // 2. La nueva entrega será tratada como entrega SIN turno y descontará stock
                        // 3. Los otros items del turno mantienen su CantidadAprobada
                        if (delivery.MedicineId.HasValue)
                        {
                            var turnoMed = turnoARevertir.Medicamentos
                                .FirstOrDefault(tm => tm.MedicineId == delivery.MedicineId.Value);
                            if (turnoMed != null)
                            {
                                _logger.LogInformation(
                                    "🔄 Limpiando CantidadAprobada de MedicineId {MedId} (era {Qty}) en turno #{TurnoId}",
                                    delivery.MedicineId.Value, turnoMed.CantidadAprobada, turnoARevertir.Id);
                                turnoMed.CantidadAprobada = null;
                            }
                        }
                        
                        if (delivery.SupplyId.HasValue)
                        {
                            var turnoIns = turnoARevertir.Insumos
                                .FirstOrDefault(ti => ti.SupplyId == delivery.SupplyId.Value);
                            if (turnoIns != null)
                            {
                                _logger.LogInformation(
                                    "🔄 Limpiando CantidadAprobada de SupplyId {SupId} (era {Qty}) en turno #{TurnoId}",
                                    delivery.SupplyId.Value, turnoIns.CantidadAprobada, turnoARevertir.Id);
                                turnoIns.CantidadAprobada = null;
                            }
                        }
                        
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation(
                            "✅ Turno #{TurnoId} revertido a Aprobado (entrega parcial: {Restantes}/{Total} items entregados). " +
                            "El item {Tipo} ID {ItemId} ahora se tratará como entrega sin turno.", 
                            turnoARevertir.Id, 
                            entregasRestantes, 
                            totalItemsTurno,
                            delivery.MedicineId.HasValue ? "Medicamento" : "Insumo",
                            delivery.MedicineId ?? delivery.SupplyId);
                    }
                    else
                    {
                        // Todas las entregas siguen activas (no debería pasar, pero por seguridad)
                        _logger.LogInformation(
                            "Turno #{TurnoId} mantiene estado porque todas las entregas ({Restantes}/{Total}) siguen activas", 
                            turnoARevertir.Id, entregasRestantes, totalItemsTurno);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al intentar revertir turno tras eliminar entrega");
            }
        }

        /// <summary>
        /// Revierte un turno completado a Pendiente cuando se elimina una entrega
        /// Limpia las cantidades aprobadas y la fecha de entrega
        /// </summary>
        private async Task RevertTurnoToPendingIfCompletedAsync(string documentoIdentidad)
        {
            try
            {
                // Normalizar documento (igual que TurnoService.HashDocument)
                documentoIdentidad = documentoIdentidad.Trim().ToUpper();
                
                // Calcular hash del documento (mismo método que usa TurnoService)
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(documentoIdentidad));
                var documentHash = Convert.ToBase64String(hashBytes);

                // Buscar turno completado con ese documento (incluir medicamentos e insumos)
                var turno = await _context.Turnos
                    .Include(t => t.Medicamentos)
                    .Include(t => t.Insumos)
                    .FirstOrDefaultAsync(t => 
                        t.DocumentoIdentidadHash == documentHash && 
                        t.Estado == "Completado");

                if (turno != null)
                {
                    // Revertir a Pendiente
                    turno.Estado = "Pendiente";
                    turno.FechaEntrega = null;
                    
                    // Limpiar cantidades aprobadas de medicamentos
                    foreach (var tm in turno.Medicamentos)
                    {
                        tm.CantidadAprobada = null;
                    }
                    
                    // Limpiar cantidades aprobadas de insumos
                    foreach (var ti in turno.Insumos)
                    {
                        ti.CantidadAprobada = null;
                    }
                    
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Turno #{TurnoId} revertido a Pendiente tras eliminar entrega. Cantidades aprobadas limpiadas.", turno.Id);
                }
            }
            catch (Exception ex)
            {
                // Log error pero no fallar la eliminación
                _logger.LogError(ex, "Error al intentar revertir turno a Pendiente tras eliminar entrega");
            }
        }
    }
}

