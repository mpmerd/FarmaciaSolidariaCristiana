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

        // GET: Donations/Edit/5
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donation = await _context.Donations
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .FirstOrDefaultAsync(d => d.Id == id);
            
            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        // POST: Donations/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,MedicineId,SupplyId,Quantity,DonationDate,DonorNote,Comments")] Donation donation)
        {
            if (id != donation.Id)
            {
                return NotFound();
            }

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
                try
                {
                    // Obtener la donación original para calcular la diferencia en el stock
                    var originalDonation = await _context.Donations
                        .AsNoTracking()
                        .FirstOrDefaultAsync(d => d.Id == id);

                    if (originalDonation != null)
                    {
                        // Revertir el stock del medicamento/insumo anterior
                        if (originalDonation.MedicineId.HasValue && originalDonation.MedicineId == donation.MedicineId)
                        {
                            var medicine = await _context.Medicines.FindAsync(originalDonation.MedicineId.Value);
                            if (medicine != null)
                            {
                                medicine.StockQuantity -= originalDonation.Quantity;
                                medicine.StockQuantity += donation.Quantity;
                            }
                        }
                        else if (originalDonation.SupplyId.HasValue && originalDonation.SupplyId == donation.SupplyId)
                        {
                            var supply = await _context.Supplies.FindAsync(originalDonation.SupplyId.Value);
                            if (supply != null)
                            {
                                supply.StockQuantity -= originalDonation.Quantity;
                                supply.StockQuantity += donation.Quantity;
                            }
                        }
                        else
                        {
                            // Si cambió el medicamento o insumo
                            if (originalDonation.MedicineId.HasValue)
                            {
                                var oldMedicine = await _context.Medicines.FindAsync(originalDonation.MedicineId.Value);
                                if (oldMedicine != null)
                                {
                                    oldMedicine.StockQuantity -= originalDonation.Quantity;
                                }
                            }
                            else if (originalDonation.SupplyId.HasValue)
                            {
                                var oldSupply = await _context.Supplies.FindAsync(originalDonation.SupplyId.Value);
                                if (oldSupply != null)
                                {
                                    oldSupply.StockQuantity -= originalDonation.Quantity;
                                }
                            }

                            // Agregar stock al nuevo medicamento o insumo
                            if (donation.MedicineId.HasValue)
                            {
                                var newMedicine = await _context.Medicines.FindAsync(donation.MedicineId.Value);
                                if (newMedicine != null)
                                {
                                    newMedicine.StockQuantity += donation.Quantity;
                                }
                            }
                            else if (donation.SupplyId.HasValue)
                            {
                                var newSupply = await _context.Supplies.FindAsync(donation.SupplyId.Value);
                                if (newSupply != null)
                                {
                                    newSupply.StockQuantity += donation.Quantity;
                                }
                            }
                        }
                    }

                    _context.Update(donation);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Donation updated: ID {DonationId}", donation.Id);
                    TempData["SuccessMessage"] = "Donación actualizada exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!DonationExists(donation.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            return View(donation);
        }

        // GET: Donations/Delete/5
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var donation = await _context.Donations
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .FirstOrDefaultAsync(d => d.Id == id);
            
            if (donation == null)
            {
                return NotFound();
            }

            return View(donation);
        }

        // POST: Donations/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var donation = await _context.Donations
                .Include(d => d.Medicine)
                .Include(d => d.Supply)
                .FirstOrDefaultAsync(d => d.Id == id);
            
            if (donation == null)
            {
                return NotFound();
            }

            // Revertir el stock del medicamento/insumo
            if (donation.MedicineId.HasValue)
            {
                var medicine = await _context.Medicines.FindAsync(donation.MedicineId.Value);
                if (medicine != null)
                {
                    medicine.StockQuantity -= donation.Quantity;
                }
            }
            else if (donation.SupplyId.HasValue)
            {
                var supply = await _context.Supplies.FindAsync(donation.SupplyId.Value);
                if (supply != null)
                {
                    supply.StockQuantity -= donation.Quantity;
                }
            }

            _context.Donations.Remove(donation);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Donation deleted: ID {DonationId}", donation.Id);
            TempData["SuccessMessage"] = "Donación eliminada exitosamente.";

            return RedirectToAction(nameof(Index));
        }

        // GET: Donations/SearchItems?query=aspirin
        [HttpGet]
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> SearchItems(string query)
        {
            if (string.IsNullOrEmpty(query) || query.Length < 3)
            {
                return Json(new { items = new List<object>() });
            }

            var medicines = await _context.Medicines
                .Where(m => m.Name.Contains(query))
                .Select(m => new { id = m.Id, name = m.Name, type = "medicine", unit = m.Unit })
                .OrderBy(m => m.name)
                .Take(10)
                .ToListAsync();

            var supplies = await _context.Supplies
                .Where(s => s.Name.Contains(query))
                .Select(s => new { id = s.Id, name = s.Name, type = "supply", unit = s.Unit })
                .OrderBy(s => s.name)
                .Take(10)
                .ToListAsync();

            var allItems = medicines.Cast<object>().Concat(supplies.Cast<object>()).ToList();

            return Json(new { items = allItems });
        }

        private bool DonationExists(int id)
        {
            return _context.Donations.Any(e => e.Id == id);
        }
    }
}
