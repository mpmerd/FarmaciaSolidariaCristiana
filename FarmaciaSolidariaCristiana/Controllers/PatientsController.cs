using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Services;

namespace FarmaciaSolidariaCristiana.Controllers
{
    [Authorize(Roles = "Admin,Farmaceutico,Viewer")] // ViewerPublic NO tiene acceso
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly IImageCompressionService _imageCompressionService;
        private readonly ILogger<PatientsController> _logger;

        public PatientsController(
            ApplicationDbContext context, 
            IWebHostEnvironment environment,
            IImageCompressionService imageCompressionService,
            ILogger<PatientsController> logger)
        {
            _context = context;
            _environment = environment;
            _imageCompressionService = imageCompressionService;
            _logger = logger;
        }

        // GET: Patients
        public async Task<IActionResult> Index()
        {
            var patients = await _context.Patients
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.RegistrationDate)
                .ToListAsync();
            return View(patients);
        }

        // GET: Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.Documents)
                .Include(p => p.Deliveries)
                    .ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (patient == null)
            {
                return NotFound();
            }

            return View(patient);
        }

        // GET: Patients/Create
        public IActionResult Create()
        {
            return View();
        }

        // GET: API endpoint to search patient by identification
        [HttpGet]
        public async Task<IActionResult> SearchByIdentification(string identification)
        {
            if (string.IsNullOrWhiteSpace(identification))
            {
                return Json(new { exists = false });
            }

            var patient = await _context.Patients
                .Include(p => p.Deliveries)
                    .ThenInclude(d => d.Medicine)
                .FirstOrDefaultAsync(p => p.IdentificationDocument == identification && p.IsActive);

            if (patient == null)
            {
                return Json(new { exists = false });
            }

            var deliveriesData = patient.Deliveries
                .OrderByDescending(d => d.DeliveryDate)
                .Take(5)
                .Select(d => new
                {
                    medicineName = d.Medicine?.Name ?? "N/A",
                    quantity = d.Quantity,
                    deliveryDate = d.DeliveryDate
                })
                .ToList();

            return Json(new
            {
                exists = true,
                id = patient.Id,
                fullName = patient.FullName,
                age = patient.Age,
                registrationDate = patient.RegistrationDate,
                deliveries = deliveriesData
            });
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Patient patient, List<IFormFile> documents, List<string> documentTypes, List<string> documentDescriptions)
        {
            if (ModelState.IsValid)
            {
                patient.RegistrationDate = DateTime.Now;
                patient.IsActive = true;

                _context.Add(patient);
                await _context.SaveChangesAsync();

                // Handle document uploads
                if (documents != null && documents.Count > 0)
                {
                    await UploadDocuments(patient.Id, documents, documentTypes, documentDescriptions);
                }

                TempData["SuccessMessage"] = "Paciente creado exitosamente.";
                return RedirectToAction(nameof(Details), new { id = patient.Id });
            }
            return View(patient);
        }

        // GET: Patients/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
            {
                return NotFound();
            }
            return View(patient);
        }

        // POST: Patients/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Patient patient, List<IFormFile> documents, List<string> documentTypes, List<string> documentDescriptions)
        {
            if (id != patient.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(patient);
                    await _context.SaveChangesAsync();

                    // Handle new document uploads
                    if (documents != null && documents.Count > 0)
                    {
                        await UploadDocuments(patient.Id, documents, documentTypes, documentDescriptions);
                    }

                    TempData["SuccessMessage"] = "Paciente actualizado exitosamente.";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PatientExists(patient.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Details), new { id = patient.Id });
            }
            return View(patient);
        }

        // POST: Patients/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.Deliveries)
                .FirstOrDefaultAsync(p => p.Id == id);
                
            if (patient == null)
            {
                return NotFound();
            }

            // Verificar si el paciente tiene entregas asociadas
            if (patient.Deliveries != null && patient.Deliveries.Any())
            {
                TempData["ErrorMessage"] = "No está permitido eliminar este paciente porque tiene entregas asignadas.";
                _logger.LogWarning("Attempted to delete patient with deliveries: {PatientName} (ID: {Id})", 
                    patient.FullName, patient.Id);
                return RedirectToAction(nameof(Index));
            }

            // Soft delete si no tiene entregas
            patient.IsActive = false;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Patient soft-deleted: {PatientName} (ID: {Id})", patient.FullName, patient.Id);
            TempData["SuccessMessage"] = "Paciente eliminado exitosamente.";

            return RedirectToAction(nameof(Index));
        }

        // POST: Patients/DeleteDocument/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int id, int patientId)
        {
            var document = await _context.PatientDocuments.FindAsync(id);
            if (document != null)
            {
                // Delete physical file
                var filePath = Path.Combine(_environment.WebRootPath, document.FilePath.TrimStart('/'));
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                _context.PatientDocuments.Remove(document);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Documento eliminado exitosamente.";
            }

            return RedirectToAction(nameof(Edit), new { id = patientId });
        }

        private bool PatientExists(int id)
        {
            return _context.Patients.Any(e => e.Id == id);
        }

        private async Task UploadDocuments(int patientId, List<IFormFile> documents, List<string> documentTypes, List<string> documentDescriptions)
        {
            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "patient-documents");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            for (int i = 0; i < documents.Count; i++)
            {
                var file = documents[i];
                if (file.Length > 0)
                {
                    var fileName = $"{patientId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    long fileSize = file.Length;
                    var originalSize = file.Length;

                    // Check if file is an image and compress it
                    if (_imageCompressionService.IsImage(file.ContentType))
                    {
                        _logger.LogInformation("Compressing image: {FileName}, Original size: {Size} bytes", 
                            file.FileName, originalSize);

                        using (var inputStream = file.OpenReadStream())
                        {
                            // Compress the image
                            using (var compressedStream = await _imageCompressionService.CompressImageAsync(
                                inputStream, 
                                file.ContentType,
                                maxWidth: 1920,    // Max width for documents
                                maxHeight: 1920,   // Max height for documents
                                quality: 85))      // Quality: 85% is a good balance
                            {
                                // Save compressed image
                                using (var fileStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await compressedStream.CopyToAsync(fileStream);
                                    fileSize = fileStream.Length;
                                }
                            }
                        }

                        var compressionRatio = originalSize > 0 ? (1 - (double)fileSize / originalSize) * 100 : 0;
                        _logger.LogInformation("Image compressed and saved: {FileName}, New size: {Size} bytes, Compression: {Ratio:F2}%",
                            fileName, fileSize, compressionRatio);
                    }
                    else
                    {
                        // Not an image, save as-is
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                            fileSize = stream.Length;
                        }
                        _logger.LogInformation("Non-image file saved: {FileName}, Size: {Size} bytes", 
                            fileName, fileSize);
                    }

                    var document = new PatientDocument
                    {
                        PatientId = patientId,
                        DocumentType = documentTypes != null && i < documentTypes.Count ? documentTypes[i] : "Otro",
                        FileName = file.FileName,
                        FilePath = $"/uploads/patient-documents/{fileName}",
                        FileSize = fileSize,
                        ContentType = file.ContentType,
                        Description = documentDescriptions != null && i < documentDescriptions.Count ? documentDescriptions[i] : null,
                        UploadDate = DateTime.Now
                    };

                    _context.PatientDocuments.Add(document);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
