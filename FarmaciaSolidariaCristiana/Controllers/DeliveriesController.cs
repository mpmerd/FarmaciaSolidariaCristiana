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
            return View(await deliveries.OrderByDescending(d => d.DeliveryDate).ToListAsync());
        }

        [Authorize(Roles = "Admin,Farmaceutico")]
        public IActionResult Create()
        {
            ViewData["MedicineId"] = new SelectList(_context.Medicines, "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PatientIdentification,MedicineId,Quantity,DeliveryDate,PatientNote,Comments,Dosage,TreatmentDuration")] Delivery delivery)
        {
            if (ModelState.IsValid)
            {
                // Buscar el paciente por su identificación
                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.IdentificationDocument == delivery.PatientIdentification && p.IsActive);

                if (patient == null)
                {
                    ModelState.AddModelError("PatientIdentification", 
                        "Paciente no encontrado. Por favor, registre primero al paciente en la sección de Pacientes.");
                    ViewData["MedicineId"] = new SelectList(_context.Medicines, "Id", "Name", delivery.MedicineId);
                    return View(delivery);
                }

                // Asignar el paciente a la entrega
                delivery.PatientId = patient.Id;
                
                // Capturar el usuario que realiza la entrega
                delivery.DeliveredBy = User.Identity?.Name ?? "Sistema";

                var medicine = await _context.Medicines.FindAsync(delivery.MedicineId);
                if (medicine == null)
                {
                    ModelState.AddModelError("", "Medicamento no encontrado");
                    ViewData["MedicineId"] = new SelectList(_context.Medicines, "Id", "Name", delivery.MedicineId);
                    return View(delivery);
                }

                if (medicine.StockQuantity < delivery.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Stock insuficiente. Disponible: {medicine.StockQuantity} {medicine.Unit}");
                    ViewData["MedicineId"] = new SelectList(_context.Medicines, "Id", "Name", delivery.MedicineId);
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
            
            ViewData["MedicineId"] = new SelectList(_context.Medicines, "Id", "Name", delivery.MedicineId);
            return View(delivery);
        }
    }
}
