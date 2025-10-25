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
            var deliveries = _context.Deliveries.Include(d => d.Medicine).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                deliveries = deliveries.Where(d => d.Medicine!.Name.Contains(searchString));
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
                .OrderBy(d => d.Medicine!.Name)
                .ThenByDescending(d => d.DeliveryDate)
                .ToListAsync());
        }

        [Authorize(Roles = "Admin,Farmaceutico")]
        public IActionResult Create()
        {
            ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientIdentification,MedicineId,Quantity,DeliveryDate,PatientNote,Comments,Dosage,TreatmentDuration")] Delivery delivery)
        {
            if (ModelState.IsValid)
            {
                // Validar que la fecha de entrega no sea futura ni más de 5 días en el pasado
                var today = DateTime.Today;
                var deliveryDateOnly = delivery.DeliveryDate.Date;
                var minAllowedDate = today.AddDays(-5);

                if (deliveryDateOnly > today)
                {
                    ModelState.AddModelError("DeliveryDate", "La fecha de entrega no puede ser futura.");
                    ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name", delivery.MedicineId);
                    return View(delivery);
                }

                if (deliveryDateOnly < minAllowedDate)
                {
                    ModelState.AddModelError("DeliveryDate", "La fecha de entrega no puede ser mayor a 5 días en el pasado.");
                    ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name", delivery.MedicineId);
                    return View(delivery);
                }

                // Buscar el paciente por su identificación
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.IdentificationDocument == delivery.PatientIdentification && p.IsActive);

                if (patient == null)
                {
                    ModelState.AddModelError("PatientIdentification", 
                        "Paciente no encontrado. Por favor, registre primero al paciente en la sección de Pacientes.");
                    ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name", delivery.MedicineId);
                    return View(delivery);
                }

                // Asignar el paciente a la entrega
                delivery.PatientId = patient.Id;
                
                // Capturar el usuario que realiza la entrega
                delivery.DeliveredBy = User.Identity?.Name ?? "Sistema";

                // Establecer la fecha de creación
                delivery.CreatedAt = DateTime.Now;

                var medicine = await _context.Medicines.FindAsync(delivery.MedicineId);
                if (medicine == null)
                {
                    ModelState.AddModelError("", "Medicamento no encontrado");
                    ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name", delivery.MedicineId);
                    return View(delivery);
                }

                if (medicine.StockQuantity < delivery.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Stock insuficiente. Disponible: {medicine.StockQuantity} {medicine.Unit}");
                    ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name", delivery.MedicineId);
                    return View(delivery);
                }

                medicine.StockQuantity -= delivery.Quantity;
                _context.Add(delivery);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Delivery created for patient: {PatientName}, medicine: {MedicineName}, Quantity: {Quantity}", 
                    patient.FullName, medicine.Name, delivery.Quantity);
                TempData["SuccessMessage"] = $"Entrega registrada exitosamente para {patient.FullName}.";
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name", delivery.MedicineId);
            return View(delivery);
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

            // Devolver el medicamento al stock
            if (delivery.Medicine != null)
            {
                delivery.Medicine.StockQuantity += delivery.Quantity;
            }

            _context.Deliveries.Remove(delivery);
            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Delivery deleted: ID {Id}, Medicine: {Medicine}, Quantity: {Quantity}", 
                delivery.Id, delivery.Medicine?.Name ?? "Unknown", delivery.Quantity);
            TempData["SuccessMessage"] = "Entrega eliminada exitosamente. El stock ha sido restaurado.";

            return RedirectToAction(nameof(Index));
        }
    }
}
