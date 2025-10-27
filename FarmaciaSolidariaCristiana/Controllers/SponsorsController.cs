using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Controllers
{
    public class SponsorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<SponsorsController> _logger;
        private readonly IImageCompressionService _imageCompressionService;

        public SponsorsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<SponsorsController> logger,
            IImageCompressionService imageCompressionService)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
            _imageCompressionService = imageCompressionService;
        }

        // GET: Sponsors (Público - muestra solo activos)
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            var sponsors = await _context.Sponsors
                .Where(s => s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return View(sponsors);
        }

        // GET: Sponsors/Manage (Admin - gestión completa)
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Manage()
        {
            var sponsors = await _context.Sponsors
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();

            return View(sponsors);
        }

        // GET: Sponsors/Create
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Sponsors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Sponsor sponsor, IFormFile? logoFile)
        {
            if (ModelState.IsValid)
            {
                // Handle logo upload
                if (logoFile != null && logoFile.Length > 0)
                {
                    // Validate file type (PNG only)
                    if (!logoFile.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
                    {
                        ModelState.AddModelError("logoFile", "Solo se permiten archivos PNG.");
                        return View(sponsor);
                    }

                    // Validate file size (max 2MB)
                    if (logoFile.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("logoFile", "El tamaño del archivo no debe superar 2MB.");
                        return View(sponsor);
                    }

                    try
                    {
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "sponsors");
                        Directory.CreateDirectory(uploadsFolder);

                        // Generate safe filename
                        var safeFileName = string.Join("_", sponsor.Name.Split(Path.GetInvalidFileNameChars()));
                        var fileName = $"{safeFileName}.png";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        // Use image compression service
                        using (var stream = logoFile.OpenReadStream())
                        {
                            using (var compressedStream = await _imageCompressionService.CompressImageAsync(
                                stream, 
                                logoFile.ContentType, 
                                maxWidth: 400, 
                                maxHeight: 400, 
                                quality: 90))
                            {
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await compressedStream.CopyToAsync(fileStream);
                                }
                            }
                        }

                        sponsor.LogoPath = $"/images/sponsors/{fileName}";
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error saving sponsor logo");
                        ModelState.AddModelError("", "Error al guardar la imagen del patrocinador.");
                        return View(sponsor);
                    }
                }
                else
                {
                    ModelState.AddModelError("logoFile", "El logo es requerido al crear un patrocinador.");
                    return View(sponsor);
                }

                sponsor.CreatedDate = DateTime.Now;
                _context.Add(sponsor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Patrocinador creado exitosamente.";
                return RedirectToAction(nameof(Manage));
            }

            return View(sponsor);
        }

        // GET: Sponsors/Edit/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor == null)
            {
                return NotFound();
            }

            return View(sponsor);
        }

        // POST: Sponsors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Sponsor sponsor, IFormFile? logoFile)
        {
            if (id != sponsor.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Handle logo upload
                    if (logoFile != null && logoFile.Length > 0)
                    {
                        // Validate file type (PNG only)
                        if (!logoFile.ContentType.Equals("image/png", StringComparison.OrdinalIgnoreCase))
                        {
                            ModelState.AddModelError("logoFile", "Solo se permiten archivos PNG.");
                            return View(sponsor);
                        }

                        // Validate file size (max 2MB)
                        if (logoFile.Length > 2 * 1024 * 1024)
                        {
                            ModelState.AddModelError("logoFile", "El tamaño del archivo no debe superar 2MB.");
                            return View(sponsor);
                        }

                        // Delete old logo if exists
                        if (!string.IsNullOrEmpty(sponsor.LogoPath))
                        {
                            var oldLogoPath = Path.Combine(_environment.WebRootPath, sponsor.LogoPath.TrimStart('/'));
                            if (System.IO.File.Exists(oldLogoPath))
                            {
                                System.IO.File.Delete(oldLogoPath);
                            }
                        }

                        // Upload new logo with compression
                        var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "sponsors");
                        Directory.CreateDirectory(uploadsFolder);

                        // Generate safe filename
                        var safeFileName = string.Join("_", sponsor.Name.Split(Path.GetInvalidFileNameChars()));
                        var fileName = $"{safeFileName}.png";
                        var filePath = Path.Combine(uploadsFolder, fileName);

                        // Use image compression service
                        using (var stream = logoFile.OpenReadStream())
                        {
                            using (var compressedStream = await _imageCompressionService.CompressImageAsync(
                                stream, 
                                logoFile.ContentType, 
                                maxWidth: 400, 
                                maxHeight: 400, 
                                quality: 90))
                            {
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await compressedStream.CopyToAsync(fileStream);
                                }
                            }
                        }

                        sponsor.LogoPath = $"/images/sponsors/{fileName}";
                    }

                    _context.Update(sponsor);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Patrocinador actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SponsorExists(sponsor.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating sponsor logo");
                    ModelState.AddModelError("", "Error al actualizar la imagen del patrocinador.");
                    return View(sponsor);
                }

                return RedirectToAction(nameof(Manage));
            }

            return View(sponsor);
        }

        // GET: Sponsors/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var sponsor = await _context.Sponsors
                .FirstOrDefaultAsync(m => m.Id == id);

            if (sponsor == null)
            {
                return NotFound();
            }

            return View(sponsor);
        }

        // POST: Sponsors/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sponsor = await _context.Sponsors.FindAsync(id);
            if (sponsor != null)
            {
                // Delete logo file if exists
                if (!string.IsNullOrEmpty(sponsor.LogoPath))
                {
                    var logoPath = Path.Combine(_environment.WebRootPath, sponsor.LogoPath.TrimStart('/'));
                    if (System.IO.File.Exists(logoPath))
                    {
                        System.IO.File.Delete(logoPath);
                    }
                }

                _context.Sponsors.Remove(sponsor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Patrocinador eliminado exitosamente.";
            }

            return RedirectToAction(nameof(Manage));
        }

        private bool SponsorExists(int id)
        {
            return _context.Sponsors.Any(e => e.Id == id);
        }
    }
}
