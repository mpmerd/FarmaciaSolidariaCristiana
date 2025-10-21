using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;

namespace FarmaciaSolidariaCristiana.Controllers
{
    [Authorize(Roles = "Admin,Farmaceutico,Viewer")] // ViewerPublic NO tiene acceso
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public PatientsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
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
            var patient = await _context.Patients.FindAsync(id);
            if (patient != null)
            {
                // Soft delete
                patient.IsActive = false;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Paciente eliminado exitosamente.";
            }

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

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    var document = new PatientDocument
                    {
                        PatientId = patientId,
                        DocumentType = documentTypes != null && i < documentTypes.Count ? documentTypes[i] : "Otro",
                        FileName = file.FileName,
                        FilePath = $"/uploads/patient-documents/{fileName}",
                        FileSize = file.Length,
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
