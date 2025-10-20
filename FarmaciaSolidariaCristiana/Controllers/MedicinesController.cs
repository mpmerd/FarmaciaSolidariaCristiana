using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using System.Text.Json;

namespace FarmaciaSolidariaCristiana.Controllers
{
    [Authorize]
    public class MedicinesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MedicinesController> _logger;

        public MedicinesController(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<MedicinesController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        // GET: Medicines
        public async Task<IActionResult> Index(string searchString)
        {
            var medicines = from m in _context.Medicines
                           select m;

            if (!string.IsNullOrEmpty(searchString))
            {
                medicines = medicines.Where(m => m.Name.Contains(searchString));
            }

            return View(await medicines.OrderBy(m => m.Name).ToListAsync());
        }

        // GET: Medicines/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicine = await _context.Medicines
                .Include(m => m.Deliveries)
                .Include(m => m.Donations)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (medicine == null)
            {
                return NotFound();
            }

            return View(medicine);
        }

        // GET: Medicines/Create
        [Authorize(Roles = "Farmaceutico")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Medicines/Create
        [HttpPost]
        [Authorize(Roles = "Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Description,StockQuantity,Unit,NationalCode")] Medicine medicine)
        {
            if (ModelState.IsValid)
            {
                _context.Add(medicine);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Medicine created: {MedicineName}", medicine.Name);
                TempData["SuccessMessage"] = $"Medicamento '{medicine.Name}' creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            return View(medicine);
        }

        // GET: Medicines/Edit/5
        [Authorize(Roles = "Farmaceutico")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
            {
                return NotFound();
            }
            return View(medicine);
        }

        // POST: Medicines/Edit/5
        [HttpPost]
        [Authorize(Roles = "Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Description,StockQuantity,Unit,NationalCode")] Medicine medicine)
        {
            if (id != medicine.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(medicine);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Medicine updated: {MedicineName}", medicine.Name);
                    TempData["SuccessMessage"] = $"Medicamento '{medicine.Name}' actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MedicineExists(medicine.Id))
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
            return View(medicine);
        }

        // GET: Medicines/Delete/5
        [Authorize(Roles = "Farmaceutico")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var medicine = await _context.Medicines
                .FirstOrDefaultAsync(m => m.Id == id);
            if (medicine == null)
            {
                return NotFound();
            }

            return View(medicine);
        }

        // POST: Medicines/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Farmaceutico")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine != null)
            {
                _context.Medicines.Remove(medicine);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Medicine deleted: {MedicineName}", medicine.Name);
                TempData["SuccessMessage"] = "Medicamento eliminado exitosamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Medicines/SearchCIMA?cn=12345
        [HttpGet]
        [Authorize(Roles = "Farmaceutico")]
        public async Task<IActionResult> SearchCIMA(string cn)
        {
            if (string.IsNullOrEmpty(cn))
            {
                return Json(new { success = false, message = "Código Nacional no proporcionado" });
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync($"https://cima.aemps.es/cima/rest/medicamento?cn={cn}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("CIMA API call successful for CN: {CN}", cn);

                    using (JsonDocument doc = JsonDocument.Parse(jsonString))
                    {
                        var root = doc.RootElement;
                        
                        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("resultados", out JsonElement resultados))
                        {
                            if (resultados.GetArrayLength() > 0)
                            {
                                var medicamento = resultados[0];
                                
                                var name = medicamento.TryGetProperty("nombre", out JsonElement nombreEl) 
                                    ? nombreEl.GetString() : "";
                                
                                var description = "";
                                if (medicamento.TryGetProperty("pactivos", out JsonElement pactivosEl))
                                {
                                    description = pactivosEl.GetString() ?? "";
                                }
                                
                                if (medicamento.TryGetProperty("formaFarmaceutica", out JsonElement formaEl) &&
                                    formaEl.TryGetProperty("nombre", out JsonElement formaNombreEl))
                                {
                                    var forma = formaNombreEl.GetString();
                                    if (!string.IsNullOrEmpty(forma))
                                    {
                                        description += !string.IsNullOrEmpty(description) ? $" - {forma}" : forma;
                                    }
                                }

                                return Json(new
                                {
                                    success = true,
                                    name = name,
                                    description = description
                                });
                            }
                        }
                    }
                }

                _logger.LogWarning("CIMA API returned no results for CN: {CN}", cn);
                return Json(new { success = false, message = "No se encontró información para este Código Nacional" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CIMA API for CN: {CN}", cn);
                return Json(new { success = false, message = "Error al conectar con CIMA API" });
            }
        }

        private bool MedicineExists(int id)
        {
            return _context.Medicines.Any(e => e.Id == id);
        }
    }
}
