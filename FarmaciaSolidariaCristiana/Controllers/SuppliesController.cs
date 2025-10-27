using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Controllers
{
    [Authorize]
    public class SuppliesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SuppliesController> _logger;

        public SuppliesController(ApplicationDbContext context, ILogger<SuppliesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: Supplies
        public async Task<IActionResult> Index(string searchString)
        {
            var supplies = from s in _context.Supplies
                          select s;

            if (!string.IsNullOrEmpty(searchString))
            {
                supplies = supplies.Where(s => s.Name.Contains(searchString));
            }

            return View(await supplies.OrderBy(s => s.Name).ToListAsync());
        }

        // GET: Supplies/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supply = await _context.Supplies
                .FirstOrDefaultAsync(m => m.Id == id);

            if (supply == null)
            {
                return NotFound();
            }

            return View(supply);
        }

        // GET: Supplies/Create
        [Authorize(Roles = "Admin,Farmaceutico")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Supplies/Create
        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,StockQuantity")] Supply supply)
        {
            if (ModelState.IsValid)
            {
                supply.Unit = "Unidades"; // Siempre usar "Unidades"
                _context.Add(supply);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Supply created: {SupplyName}", supply.Name);
                TempData["SuccessMessage"] = $"Insumo '{supply.Name}' creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(supply);
        }

        // GET: Supplies/Edit/5
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supply = await _context.Supplies.FindAsync(id);
            if (supply == null)
            {
                return NotFound();
            }
            return View(supply);
        }

        // POST: Supplies/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,StockQuantity")] Supply supply)
        {
            if (id != supply.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    supply.Unit = "Unidades"; // Siempre usar "Unidades"
                    _context.Update(supply);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Supply updated: {SupplyName}", supply.Name);
                    TempData["SuccessMessage"] = $"Insumo '{supply.Name}' actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SupplyExists(supply.Id))
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
            return View(supply);
        }

        // GET: Supplies/Delete/5
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var supply = await _context.Supplies
                .FirstOrDefaultAsync(m => m.Id == id);
            if (supply == null)
            {
                return NotFound();
            }

            return View(supply);
        }

        // POST: Supplies/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var supply = await _context.Supplies.FindAsync(id);
            if (supply != null)
            {
                _context.Supplies.Remove(supply);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Supply deleted: {SupplyName}", supply.Name);
                TempData["SuccessMessage"] = "Insumo eliminado exitosamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool SupplyExists(int id)
        {
            return _context.Supplies.Any(e => e.Id == id);
        }
    }
}
