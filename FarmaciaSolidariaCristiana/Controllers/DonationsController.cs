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
            var donations = _context.Donations.Include(d => d.Medicine).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                donations = donations.Where(d => d.Medicine!.Name.Contains(searchString));
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
            return View(await donations.OrderByDescending(d => d.DonationDate).ToListAsync());
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
        public async Task<IActionResult> Create([Bind("MedicineId,Quantity,DonationDate,DonorNote,Comments")] Donation donation)
        {
            if (ModelState.IsValid)
            {
                var medicine = await _context.Medicines.FindAsync(donation.MedicineId);
                if (medicine != null)
                {
                    medicine.StockQuantity += donation.Quantity;
                    _context.Add(donation);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Donation created for medicine: {MedicineName}, Quantity: {Quantity}", medicine.Name, donation.Quantity);
                    TempData["SuccessMessage"] = "Donación registrada exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
            }
            
            ViewData["MedicineId"] = new SelectList(_context.Medicines, "Id", "Name", donation.MedicineId);
            return View(donation);
        }
    }
}
