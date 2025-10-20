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

        [Authorize(Roles = "Farmaceutico")]
        public IActionResult Create()
        {
            ViewData["MedicineId"] = new SelectList(_context.Medicines, "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MedicineId,Quantity,DeliveryDate,PatientNote,Comments")] Delivery delivery)
        {
            if (ModelState.IsValid)
            {
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
                
                _logger.LogInformation("Delivery created for medicine: {MedicineName}, Quantity: {Quantity}", medicine.Name, delivery.Quantity);
                TempData["SuccessMessage"] = "Entrega registrada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["MedicineId"] = new SelectList(_context.Medicines, "Id", "Name", delivery.MedicineId);
            return View(delivery);
        }
    }
}
