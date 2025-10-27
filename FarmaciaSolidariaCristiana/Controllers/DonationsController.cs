using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Controllers
{
    [Authorize]
    public class DonationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DonationsController> _logger;

        public DonationsController(ApplicationDbContext context, ILogger<DonationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string searchString, DateTime? startDate, DateTime? endDate)
        {
            var donations = _context.Donations
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                donations = donations.Where(d => 
                    (d.Medicine != null && d.Medicine.Name.Contains(searchString)) ||
                    (d.Supply != null && d.Supply.Name.Contains(searchString)));
            }

            if (startDate.HasValue)
            {
                donations = donations.Where(d => d.DonationDate >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                donations = donations.Where(d => d.DonationDate <= endDate.Value);
            }

            ViewData["TotalDonations"] = await donations.SumAsync(d => d.Quantity);
            return View(await donations
                .OrderByDescending(d => d.DonationDate)
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
        public async Task<IActionResult> Create([Bind("MedicineId,SupplyId,Quantity,DonationDate,DonorNote,Comments")] Donation donation)
        {
            // Validar que se haya seleccionado medicamento O insumo (pero no ambos ni ninguno)
            if (!donation.MedicineId.HasValue && !donation.SupplyId.HasValue)
            {
                ModelState.AddModelError("", "Debe seleccionar un medicamento o un insumo.");
            }
            else if (donation.MedicineId.HasValue && donation.SupplyId.HasValue)
            {
                ModelState.AddModelError("", "Solo puede seleccionar medicamento O insumo, no ambos.");
            }

            if (ModelState.IsValid)
            {
                string itemName = "";
                string itemUnit = "";

                if (donation.MedicineId.HasValue)
                {
                    var medicine = await _context.Medicines.FindAsync(donation.MedicineId.Value);
                    if (medicine == null)
                    {
                        ModelState.AddModelError("", "Medicamento no encontrado");
                        ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name", donation.MedicineId);
                        ViewData["SupplyId"] = new SelectList(_context.Supplies.OrderBy(s => s.Name), "Id", "Name", donation.SupplyId);
                        return View(donation);
                    }

                    medicine.StockQuantity += donation.Quantity;
                    itemName = medicine.Name;
                    itemUnit = medicine.Unit;
                }
                else if (donation.SupplyId.HasValue)
                {
                    var supply = await _context.Supplies.FindAsync(donation.SupplyId.Value);
                    if (supply == null)
                    {
                        ModelState.AddModelError("", "Insumo no encontrado");
                        ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name", donation.MedicineId);
                        ViewData["SupplyId"] = new SelectList(_context.Supplies.OrderBy(s => s.Name), "Id", "Name", donation.SupplyId);
                        return View(donation);
                    }

                    supply.StockQuantity += donation.Quantity;
                    itemName = supply.Name;
                    itemUnit = supply.Unit;
                }

                _context.Add(donation);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Donation created for item: {ItemName}, Quantity: {Quantity}", itemName, donation.Quantity);
                TempData["SuccessMessage"] = "Donación registrada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            
            ViewData["MedicineId"] = new SelectList(_context.Medicines.OrderBy(m => m.Name), "Id", "Name", donation.MedicineId);
            ViewData["SupplyId"] = new SelectList(_context.Supplies.OrderBy(s => s.Name), "Id", "Name", donation.SupplyId);
            return View(donation);
        }
    }
}
