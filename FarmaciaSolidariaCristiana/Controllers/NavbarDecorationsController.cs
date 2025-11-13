using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using System.Security.Claims;

namespace FarmaciaSolidariaCristiana.Controllers
{
    [Authorize(Roles = "Admin")]
    public class NavbarDecorationsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<NavbarDecorationsController> _logger;

        public NavbarDecorationsController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<NavbarDecorationsController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET: NavbarDecorations
        public async Task<IActionResult> Index()
        {
            var decorations = await _context.NavbarDecorations
                .OrderByDescending(d => d.IsActive)
                .ThenByDescending(d => d.CreatedAt)
                .ToListAsync();

            ViewBag.Presets = PresetDecorations.Presets;
            return View(decorations);
        }

        // GET: API endpoint para obtener decoración activa (público, sin autenticación)
        [AllowAnonymous]
        [HttpGet("/api/navbar-decoration/active")]
        public async Task<IActionResult> GetActiveDecoration()
        {
            var activeDecoration = await _context.NavbarDecorations
                .FirstOrDefaultAsync(d => d.IsActive);

            if (activeDecoration == null)
            {
                return Ok(new { active = false });
            }

            var response = new
            {
                active = true,
                name = activeDecoration.Name,
                displayText = activeDecoration.DisplayText,
                textColor = activeDecoration.TextColor ?? "#FFFFFF",
                iconClass = activeDecoration.IconClass,
                iconColor = activeDecoration.IconColor ?? "#FFFFFF",
                customIconPath = activeDecoration.CustomIconPath,
                type = activeDecoration.Type.ToString()
            };

            return Ok(response);
        }

        // POST: Activar decoración predefinida
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivatePreset(string presetKey, string? customText)
        {
            if (!PresetDecorations.Presets.ContainsKey(presetKey))
            {
                TempData["ErrorMessage"] = "Decoración no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            var preset = PresetDecorations.Presets[presetKey];
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "Admin";

            // Desactivar cualquier decoración activa
            await DeactivateAllDecorationsAsync();

            // Buscar si ya existe una decoración para este preset
            var existingDecoration = await _context.NavbarDecorations
                .FirstOrDefaultAsync(d => d.PresetKey == presetKey);

            if (existingDecoration != null)
            {
                // Actualizar y activar la existente
                existingDecoration.IsActive = true;
                existingDecoration.DisplayText = string.IsNullOrWhiteSpace(customText) 
                    ? preset.DefaultText 
                    : customText;
                existingDecoration.ActivatedAt = DateTime.Now;
                existingDecoration.ActivatedBy = userName;
            }
            else
            {
                // Crear nueva decoración
                var decoration = new NavbarDecoration
                {
                    Name = preset.Name,
                    Type = DecorationType.Predefined,
                    PresetKey = presetKey,
                    DisplayText = string.IsNullOrWhiteSpace(customText) 
                        ? preset.DefaultText 
                        : customText,
                    TextColor = preset.TextColor,
                    IconClass = preset.IconClass,
                    IconColor = preset.IconColor,
                    IsActive = true,
                    ActivatedAt = DateTime.Now,
                    ActivatedBy = userName,
                    CreatedAt = DateTime.Now
                };

                _context.NavbarDecorations.Add(decoration);
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Decoración '{PresetName}' activada por {User}", 
                preset.Name, userName);

            TempData["SuccessMessage"] = $"¡Decoración '{preset.Name}' activada exitosamente!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Activar decoración personalizada
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ActivateCustom(
            string name,
            string? displayText,
            string? textColor,
            string? iconColor,
            IFormFile? iconFile)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                TempData["ErrorMessage"] = "El nombre de la decoración es obligatorio.";
                return RedirectToAction(nameof(Index));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = User.Identity?.Name ?? "Admin";

            // Desactivar cualquier decoración activa
            await DeactivateAllDecorationsAsync();

            string? iconPath = null;

            // Procesar archivo de icono si se subió
            if (iconFile != null && iconFile.Length > 0)
            {
                var allowedExtensions = new[] { ".png", ".jpg", ".jpeg", ".svg", ".gif" };
                var extension = Path.GetExtension(iconFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    TempData["ErrorMessage"] = "Solo se permiten imágenes (PNG, JPG, SVG, GIF).";
                    return RedirectToAction(nameof(Index));
                }

                // Tamaño máximo: 1MB
                if (iconFile.Length > 1 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "El icono no debe superar 1MB.";
                    return RedirectToAction(nameof(Index));
                }

                // Guardar el archivo
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "decorations");
                Directory.CreateDirectory(uploadsFolder);

                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await iconFile.CopyToAsync(stream);
                }

                iconPath = $"/uploads/decorations/{uniqueFileName}";
            }

            var decoration = new NavbarDecoration
            {
                Name = name,
                Type = DecorationType.Custom,
                DisplayText = displayText,
                TextColor = textColor ?? "#FFFFFF",
                IconColor = iconColor ?? "#FFFFFF",
                CustomIconPath = iconPath,
                IsActive = true,
                ActivatedAt = DateTime.Now,
                ActivatedBy = userName,
                CreatedAt = DateTime.Now
            };

            _context.NavbarDecorations.Add(decoration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Decoración personalizada '{Name}' activada por {User}", 
                name, userName);

            TempData["SuccessMessage"] = $"¡Decoración personalizada '{name}' activada exitosamente!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Desactivar todas las decoraciones
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateAll()
        {
            await DeactivateAllDecorationsAsync();
            await _context.SaveChangesAsync();

            _logger.LogInformation("Todas las decoraciones desactivadas por {User}", 
                User.Identity?.Name ?? "Admin");

            TempData["SuccessMessage"] = "Todas las decoraciones han sido desactivadas.";
            return RedirectToAction(nameof(Index));
        }

        // DELETE: Eliminar decoración
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var decoration = await _context.NavbarDecorations.FindAsync(id);
            if (decoration == null)
            {
                TempData["ErrorMessage"] = "Decoración no encontrada.";
                return RedirectToAction(nameof(Index));
            }

            // Eliminar archivo de icono personalizado si existe
            if (decoration.Type == DecorationType.Custom && 
                !string.IsNullOrEmpty(decoration.CustomIconPath))
            {
                var filePath = Path.Combine(_environment.WebRootPath, 
                    decoration.CustomIconPath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }

            _context.NavbarDecorations.Remove(decoration);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Decoración '{Name}' eliminada por {User}", 
                decoration.Name, User.Identity?.Name ?? "Admin");

            TempData["SuccessMessage"] = "Decoración eliminada exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar para desactivar todas las decoraciones
        private async Task DeactivateAllDecorationsAsync()
        {
            var activeDecorations = await _context.NavbarDecorations
                .Where(d => d.IsActive)
                .ToListAsync();

            foreach (var decoration in activeDecorations)
            {
                decoration.IsActive = false;
            }
        }
    }
}
