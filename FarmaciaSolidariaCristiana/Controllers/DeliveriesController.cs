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

            try
            {
                var deliveredBy = User.Identity?.Name ?? "Sistema";
                var createdAt = DateTime.Now;
                int deliveriesCount = 0;

                // ✅ NUEVO: Determinar el TurnoId ANTES de crear las entregas
                int? turnoId = null;
                if (hasMedicines)
                {
                    var firstMedicineId = MedicineIds!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).First();
                    turnoId = await FindTurnoIdAsync(PatientIdentification, firstMedicineId, null);
                }
                else if (hasSupplies)
                {
                    var firstSupplyId = SupplyIds!.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).First();
                    turnoId = await FindTurnoIdAsync(PatientIdentification, null, firstSupplyId);
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
                        var medicine = await _context.Medicines.FindAsync(medicineIdsList[i]);
                        if (medicine == null)
                        {
                            TempData["ErrorMessage"] = $"Medicamento con ID {medicineIdsList[i]} no encontrado.";
                            return RedirectToAction(nameof(Create));
                        }

                        if (medicine.StockQuantity < medicineQuantitiesList[i])
                        {
                            TempData["ErrorMessage"] = $"Stock insuficiente para {medicine.Name}. Disponible: {medicine.StockQuantity} {medicine.Unit}";
                            return RedirectToAction(nameof(Create));
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

                        // ✅ CORREGIDO: Solo descontar stock si NO es de un turno aprobado
                        // Si es de un turno, el stock ya fue reservado al aprobar
                        if (turnoId == null)
                        {
                            medicine.StockQuantity -= medicineQuantitiesList[i];
                            _logger.LogInformation("Stock descontado (entrega sin turno) - Medicine: {MedicineName}, Quantity: {Quantity}",
                                medicine.Name, medicineQuantitiesList[i]);
                        }
                        else
                        {
                            _logger.LogInformation("Stock YA reservado (entrega de turno #{TurnoId}) - Medicine: {MedicineName}, Quantity: {Quantity}",
                                turnoId, medicine.Name, medicineQuantitiesList[i]);
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
                        var supply = await _context.Supplies.FindAsync(supplyIdsList[i]);
                        if (supply == null)
                        {
                            TempData["ErrorMessage"] = $"Insumo con ID {supplyIdsList[i]} no encontrado.";
                            return RedirectToAction(nameof(Create));
                        }

                        if (supply.StockQuantity < supplyQuantitiesList[i])
                        {
                            TempData["ErrorMessage"] = $"Stock insuficiente para {supply.Name}. Disponible: {supply.StockQuantity} {supply.Unit}";
                            return RedirectToAction(nameof(Create));
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

                        // ✅ CORREGIDO: Solo descontar stock si NO es de un turno aprobado
                        // Si es de un turno, el stock ya fue reservado al aprobar
                        if (turnoId == null)
                        {
                            supply.StockQuantity -= supplyQuantitiesList[i];
                            _logger.LogInformation("Stock descontado (entrega sin turno) - Supply: {SupplyName}, Quantity: {Quantity}",
                                supply.Name, supplyQuantitiesList[i]);
                        }
                        else
                        {
                            _logger.LogInformation("Stock YA reservado (entrega de turno #{TurnoId}) - Supply: {SupplyName}, Quantity: {Quantity}",
                                turnoId, supply.Name, supplyQuantitiesList[i]);
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

                TempData["SuccessMessage"] = $"{deliveriesCount} entrega(s) registrada(s) exitosamente para {patient.FullName}.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating deliveries");
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
        /// Busca el ID del turno que contiene el medicamento o insumo especificado
        /// SIN marcar como completado (solo buscar)
        /// </summary>
        private async Task<int?> FindTurnoIdAsync(string documentoIdentidad, int? medicineId, int? supplyId)
        {
            try
            {
                // Calcular hash del documento
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(documentoIdentidad));
                var documentHash = Convert.ToBase64String(hashBytes);

                // Buscar turnos aprobados o pendientes
                var turnos = await _context.Turnos
                    .Include(t => t.Medicamentos)
                    .Include(t => t.Insumos)
                    .Where(t => 
                        t.DocumentoIdentidadHash == documentHash && 
                        (t.Estado == "Aprobado" || t.Estado == "Pendiente" || t.Estado == "Completado"))
                    .ToListAsync();

                if (!turnos.Any())
                {
                    return null;
                }

                // Buscar el turno que contiene el item
                Turno? turno = null;
                
                if (medicineId.HasValue)
                {
                    turno = turnos.FirstOrDefault(t => 
                        t.Medicamentos.Any(tm => tm.MedicineId == medicineId.Value));
                }
                else if (supplyId.HasValue)
                {
                    turno = turnos.FirstOrDefault(t => 
                        t.Insumos.Any(ti => ti.SupplyId == supplyId.Value));
                }

                return turno?.Id;
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
                // Calcular hash del documento (mismo método que usa TurnoService)
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(documentoIdentidad));
                var documentHash = Convert.ToBase64String(hashBytes);

                // Buscar TODOS los turnos (Aprobado o Pendiente) para este paciente
                var turnos = await _context.Turnos
                    .Include(t => t.Medicamentos)
                    .Include(t => t.Insumos)
                    .Where(t => 
                        t.DocumentoIdentidadHash == documentHash && 
                        (t.Estado == "Aprobado" || t.Estado == "Pendiente"))
                    .ToListAsync();

                if (!turnos.Any())
                {
                    return null; // No hay turnos para este paciente
                }

                // Buscar el turno ESPECÍFICO que contiene el item de la entrega
                Turno? turno = null;
                
                if (medicineId.HasValue)
                {
                    turno = turnos.FirstOrDefault(t => 
                        t.Medicamentos.Any(tm => tm.MedicineId == medicineId.Value));
                }
                else if (supplyId.HasValue)
                {
                    turno = turnos.FirstOrDefault(t => 
                        t.Insumos.Any(ti => ti.SupplyId == supplyId.Value));
                }

                if (turno != null)
                {
                    // ✅ Asegurar que las cantidades aprobadas estén establecidas
                    foreach (var tm in turno.Medicamentos)
                    {
                        if (!tm.CantidadAprobada.HasValue)
                        {
                            tm.CantidadAprobada = tm.CantidadSolicitada;
                        }
                    }
                    
                    foreach (var ti in turno.Insumos)
                    {
                        if (!ti.CantidadAprobada.HasValue)
                        {
                            ti.CantidadAprobada = ti.CantidadSolicitada;
                        }
                    }
                    
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
                    // ✅ NUEVA LÓGICA: Verificar si quedan más entregas del turno
                    // Contar cuántas entregas quedan para los items de este turno
                    int entregasRestantes = 0;

                    // Contar entregas de medicamentos del turno
                    foreach (var tm in turnoARevertir.Medicamentos)
                    {
                        var entregas = await _context.Deliveries
                            .Where(d => 
                                d.PatientIdentification == delivery.PatientIdentification &&
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
                                d.PatientIdentification == delivery.PatientIdentification &&
                                d.SupplyId == ti.SupplyId &&
                                d.Id != delivery.Id) // Excluir la entrega que se está eliminando
                            .CountAsync();
                        entregasRestantes += entregas;
                    }

                    // ✅ SOLO revertir si NO quedan más entregas
                    if (entregasRestantes == 0)
                    {
                        // Revertir este turno específico a Pendiente
                        turnoARevertir.Estado = "Pendiente";
                        turnoARevertir.FechaEntrega = null;
                        
                        // Limpiar cantidades aprobadas
                        foreach (var tm in turnoARevertir.Medicamentos)
                        {
                            tm.CantidadAprobada = null;
                        }
                        
                        foreach (var ti in turnoARevertir.Insumos)
                        {
                            ti.CantidadAprobada = null;
                        }
                        
                        await _context.SaveChangesAsync();
                        
                        _logger.LogInformation("Turno #{TurnoId} revertido a Pendiente tras eliminar ÚLTIMA entrega de {Tipo} ID {ItemId}", 
                            turnoARevertir.Id, 
                            delivery.MedicineId.HasValue ? "Medicamento" : "Insumo",
                            delivery.MedicineId ?? delivery.SupplyId);
                    }
                    else
                    {
                        _logger.LogInformation("Turno #{TurnoId} mantiene estado Completado porque quedan {EntregasRestantes} entrega(s) asociadas", 
                            turnoARevertir.Id, entregasRestantes);
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

