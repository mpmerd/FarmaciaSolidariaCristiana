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

        /// <summary>
        /// Obtiene los turnos aprobados o pendientes de un paciente por su documento de identidad
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetApprovedTurnos(string identification)
        {
            if (string.IsNullOrWhiteSpace(identification))
            {
                return Json(new { success = false, turnos = new List<object>() });
            }

            // Calcular hash del documento
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(identification));
            var documentHash = Convert.ToBase64String(hashBytes);

            // Buscar turnos aprobados o pendientes (que hayan sido revertidos)
            var turnos = await _context.Turnos
                .Include(t => t.Medicamentos)
                    .ThenInclude(tm => tm.Medicine)
                .Include(t => t.Insumos)
                    .ThenInclude(ti => ti.Supply)
                .Where(t => 
                    t.DocumentoIdentidadHash == documentHash && 
                    (t.Estado == "Aprobado" || t.Estado == "Pendiente"))
                .OrderByDescending(t => t.FechaPreferida ?? t.FechaSolicitud)
                .Select(t => new
                {
                    id = t.Id,
                    estado = t.Estado,
                    fechaTurno = t.FechaPreferida,
                    horaTurno = t.FechaPreferida != null ? t.FechaPreferida.Value.ToString("HH:mm") : null,
                    medicamentos = t.Medicamentos.Select(tm => new
                    {
                        id = tm.MedicineId,
                        nombre = tm.Medicine != null ? tm.Medicine.Name : "N/A",
                        cantidadSolicitada = tm.CantidadSolicitada,
                        cantidadAprobada = tm.CantidadAprobada,
                        unidad = tm.Medicine != null ? tm.Medicine.Unit : ""
                    }).ToList(),
                    insumos = t.Insumos.Select(ti => new
                    {
                        id = ti.SupplyId,
                        nombre = ti.Supply != null ? ti.Supply.Name : "N/A",
                        cantidadSolicitada = ti.CantidadSolicitada,
                        cantidadAprobada = ti.CantidadAprobada,
                        unidad = ti.Supply != null ? ti.Supply.Unit : ""
                    }).ToList()
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                turnos = turnos
            });
        }

        /// <summary>
        /// Busca documentos de turnos por número de identificación para importar a ficha del paciente
        /// Solo funciona cuando se está creando un nuevo paciente
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> SearchTurnoDocuments(string identification)
        {
            if (string.IsNullOrWhiteSpace(identification))
            {
                return Json(new { success = false, documents = new List<object>(), message = "Identificación vacía" });
            }

            // Calcular hash del documento
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(identification));
            var documentHash = Convert.ToBase64String(hashBytes);

            // Buscar turnos con documentos para esta identificación
            var turnosConDocumentos = await _context.Turnos
                .Include(t => t.Documentos)
                .Where(t => t.DocumentoIdentidadHash == documentHash)
                .OrderByDescending(t => t.FechaSolicitud)
                .ToListAsync();

            // Recopilar todos los documentos de todos los turnos
            var documentos = new List<object>();
            
            foreach (var turno in turnosConDocumentos)
            {
                // Documentos de la nueva tabla TurnoDocumentos
                if (turno.Documentos != null && turno.Documentos.Any())
                {
                    foreach (var doc in turno.Documentos)
                    {
                        documentos.Add(new
                        {
                            id = doc.Id,
                            turnoId = turno.Id,
                            type = doc.DocumentType,
                            fileName = doc.FileName,
                            filePath = doc.FilePath,
                            fechaSolicitud = turno.FechaSolicitud.ToString("dd/MM/yyyy"),
                            source = "turno_documento"
                        });
                    }
                }

                // También incluir los campos antiguos RecetaMedicaPath y TarjetonPath si existen
                if (!string.IsNullOrEmpty(turno.RecetaMedicaPath))
                {
                    documentos.Add(new
                    {
                        id = 0,
                        turnoId = turno.Id,
                        type = "Receta Médica",
                        fileName = Path.GetFileName(turno.RecetaMedicaPath),
                        filePath = turno.RecetaMedicaPath,
                        fechaSolicitud = turno.FechaSolicitud.ToString("dd/MM/yyyy"),
                        source = "turno_receta"
                    });
                }

                if (!string.IsNullOrEmpty(turno.TarjetonPath))
                {
                    documentos.Add(new
                    {
                        id = 0,
                        turnoId = turno.Id,
                        type = "Tarjetón Sanitario",
                        fileName = Path.GetFileName(turno.TarjetonPath),
                        filePath = turno.TarjetonPath,
                        fechaSolicitud = turno.FechaSolicitud.ToString("dd/MM/yyyy"),
                        source = "turno_tarjeton"
                    });
                }
            }

            return Json(new
            {
                success = documentos.Any(),
                documents = documentos,
                count = documentos.Count,
                message = documentos.Any() 
                    ? $"Se encontraron {documentos.Count} documento(s) en solicitudes de turno anteriores." 
                    : "No se encontraron documentos de turnos para esta identificación."
            });
        }

        /// <summary>
        /// Copia documentos de turnos a la carpeta de documentos de pacientes
        /// Retorna los IDs de las rutas copiadas para poder eliminarlas si se cancela
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> ImportTurnoDocuments([FromBody] ImportDocumentsRequest request)
        {
            if (request?.DocumentPaths == null || !request.DocumentPaths.Any())
            {
                return Json(new { success = false, message = "No se especificaron documentos para importar" });
            }

            var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "patient-documents");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var copiedFiles = new List<object>();
            var errors = new List<string>();

            foreach (var docPath in request.DocumentPaths)
            {
                try
                {
                    // Construir ruta del archivo origen (en turnos)
                    var sourcePath = Path.Combine(_environment.WebRootPath, docPath.TrimStart('/'));
                    
                    if (!System.IO.File.Exists(sourcePath))
                    {
                        _logger.LogWarning("Archivo no encontrado para importar: {Path}", sourcePath);
                        errors.Add($"Archivo no encontrado: {Path.GetFileName(docPath)}");
                        continue;
                    }

                    // Generar nuevo nombre para el archivo copiado
                    var fileExtension = Path.GetExtension(docPath).ToLower();
                    var newFileName = $"imported_{Guid.NewGuid()}{fileExtension}";
                    var destPath = Path.Combine(uploadFolder, newFileName);

                    // Copiar el archivo
                    System.IO.File.Copy(sourcePath, destPath);
                    
                    // Obtener información del archivo
                    var fileInfo = new FileInfo(destPath);

                    copiedFiles.Add(new
                    {
                        originalPath = docPath,
                        newPath = $"/uploads/patient-documents/{newFileName}",
                        fileName = Path.GetFileName(docPath),
                        fileSize = fileInfo.Length,
                        documentType = request.DocumentTypes?.ElementAtOrDefault(request.DocumentPaths.IndexOf(docPath)) ?? "Importado de Turno"
                    });

                    _logger.LogInformation("Documento importado de turno: {Source} -> {Dest}", sourcePath, destPath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importando documento: {Path}", docPath);
                    errors.Add($"Error al importar {Path.GetFileName(docPath)}: {ex.Message}");
                }
            }

            return Json(new
            {
                success = copiedFiles.Any(),
                copiedFiles = copiedFiles,
                errors = errors,
                message = copiedFiles.Any() 
                    ? $"Se importaron {copiedFiles.Count} documento(s) correctamente." 
                    : "No se pudo importar ningún documento."
            });
        }

        /// <summary>
        /// Elimina documentos importados temporalmente si se cancela la creación del paciente
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        public IActionResult CleanupImportedDocuments([FromBody] CleanupDocumentsRequest request)
        {
            if (request?.FilePaths == null || !request.FilePaths.Any())
            {
                return Json(new { success = true, message = "No hay archivos para limpiar" });
            }

            var deletedCount = 0;
            var errors = new List<string>();

            foreach (var filePath in request.FilePaths)
            {
                try
                {
                    // Solo permitir eliminar archivos de patient-documents que comiencen con "imported_"
                    if (!filePath.Contains("/uploads/patient-documents/imported_"))
                    {
                        _logger.LogWarning("Intento de eliminar archivo no permitido: {Path}", filePath);
                        continue;
                    }

                    var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                    
                    if (System.IO.File.Exists(fullPath))
                    {
                        System.IO.File.Delete(fullPath);
                        deletedCount++;
                        _logger.LogInformation("Documento importado limpiado: {Path}", fullPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error limpiando documento importado: {Path}", filePath);
                    errors.Add($"Error al eliminar {Path.GetFileName(filePath)}");
                }
            }

            return Json(new
            {
                success = true,
                deletedCount = deletedCount,
                errors = errors,
                message = $"Se limpiaron {deletedCount} archivo(s) importados temporalmente."
            });
        }

        // POST: Patients/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            Patient patient, 
            List<IFormFile> documents, 
            List<string> documentTypes, 
            List<string> documentDescriptions,
            List<string>? importedDocumentPaths,
            List<string>? importedDocumentTypes,
            List<string>? importedDocumentNames)
        {
            if (ModelState.IsValid)
            {
                patient.RegistrationDate = DateTime.Now;
                patient.IsActive = true;

                _context.Add(patient);
                await _context.SaveChangesAsync();

                // Handle document uploads (nuevos archivos)
                if (documents != null && documents.Count > 0)
                {
                    await UploadDocuments(patient.Id, documents, documentTypes, documentDescriptions);
                }
                
                // Handle imported documents from turnos (ya copiados al servidor)
                if (importedDocumentPaths != null && importedDocumentPaths.Count > 0)
                {
                    await SaveImportedDocuments(patient.Id, importedDocumentPaths, importedDocumentTypes, importedDocumentNames);
                }

                TempData["SuccessMessage"] = "Paciente creado exitosamente.";
                return RedirectToAction(nameof(Details), new { id = patient.Id });
            }
            return View(patient);
        }

        // GET: Patients/Edit/5
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var patient = await _context.Patients
                .AsNoTracking() // No track on GET to avoid conflicts
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
        [Authorize(Roles = "Admin,Farmaceutico")]
        public async Task<IActionResult> Edit(int id, Patient patient, List<IFormFile>? documents, List<string>? documentTypes, List<string>? documentDescriptions)
        {
            if (id != patient.Id)
            {
                return NotFound();
            }

            // Validar solo los campos importantes (ModelState puede tener errores de campos opcionales)
            if (string.IsNullOrWhiteSpace(patient.FullName))
            {
                TempData["ErrorMessage"] = "El nombre completo es obligatorio.";
                return RedirectToAction(nameof(Edit), new { id = id });
            }

            if (patient.Age < 0 || patient.Age > 150)
            {
                TempData["ErrorMessage"] = "La edad debe estar entre 0 y 150.";
                return RedirectToAction(nameof(Edit), new { id = id });
            }

            if (string.IsNullOrEmpty(patient.Gender) || (patient.Gender != "M" && patient.Gender != "F"))
            {
                TempData["ErrorMessage"] = "Debe seleccionar un género válido.";
                return RedirectToAction(nameof(Edit), new { id = id });
            }

            try
            {
                // Obtener el paciente existente
                var existingPatient = await _context.Patients.FindAsync(id);
                if (existingPatient == null)
                {
                    _logger.LogWarning("Patient not found: {Id}", id);
                    return NotFound();
                }

                // Actualizar solo las propiedades editables (preservar campos como RegistrationDate)
                existingPatient.FullName = patient.FullName.Trim();
                existingPatient.Age = patient.Age;
                existingPatient.Gender = patient.Gender;
                existingPatient.Address = patient.Address?.Trim();
                existingPatient.Phone = patient.Phone?.Trim();
                existingPatient.Municipality = patient.Municipality?.Trim();
                existingPatient.Province = patient.Province?.Trim();
                existingPatient.MainDiagnosis = patient.MainDiagnosis?.Trim();
                existingPatient.AssociatedPathologies = patient.AssociatedPathologies?.Trim();
                existingPatient.KnownAllergies = patient.KnownAllergies?.Trim();
                existingPatient.CurrentTreatments = patient.CurrentTreatments?.Trim();
                existingPatient.BloodPressureSystolic = patient.BloodPressureSystolic;
                existingPatient.BloodPressureDiastolic = patient.BloodPressureDiastolic;
                existingPatient.Weight = patient.Weight;
                existingPatient.Height = patient.Height;
                existingPatient.Observations = patient.Observations?.Trim();

                _logger.LogInformation("Updating patient ID: {Id}, FullName: {FullName}, Age: {Age}", 
                    id, existingPatient.FullName, existingPatient.Age);

                // Guardar cambios
                var changes = await _context.SaveChangesAsync();
                _logger.LogInformation("Patient updated successfully: {PatientName} (ID: {Id}), {Changes} changes saved", 
                    existingPatient.FullName, id, changes);

                // Handle new document uploads
                if (documents != null && documents.Count > 0)
                {
                    await UploadDocuments(id, documents, documentTypes ?? new List<string>(), documentDescriptions ?? new List<string>());
                }

                TempData["SuccessMessage"] = $"Paciente actualizado exitosamente. {changes} cambios guardados.";
                return RedirectToAction(nameof(Details), new { id = id });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _logger.LogError(ex, "Concurrency error updating patient ID: {Id}", id);
                if (!PatientExists(id))
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
                _logger.LogError(ex, "Error updating patient ID: {Id}, Message: {Message}", id, ex.Message);
                TempData["ErrorMessage"] = $"Error al actualizar el paciente: {ex.Message}";
                return RedirectToAction(nameof(Edit), new { id = id });
            }
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
                    var fileExtension = Path.GetExtension(file.FileName).ToLower();
                    
                    // Rechazar archivos HEIC ya que no son compatibles con navegadores web
                    if (fileExtension == ".heic" || fileExtension == ".heif")
                    {
                        _logger.LogWarning("HEIC file rejected: {FileName}. User must convert to JPG/PNG first.", file.FileName);
                        TempData["ErrorMessage"] = $"El archivo '{file.FileName}' tiene formato HEIC que no es compatible con navegadores web. Por favor, conviértelo a JPG o PNG antes de subirlo. En iOS/Mac puedes exportar la foto como JPG desde la app de Fotos.";
                        continue; // Saltar este archivo
                    }
                    
                    var fileName = $"{patientId}_{Guid.NewGuid()}{fileExtension}";
                    var filePath = Path.Combine(uploadFolder, fileName);

                    long fileSize = file.Length;
                    var originalSize = file.Length;
                    string contentType = file.ContentType;

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
                                contentType,
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
                        ContentType = contentType,
                        Description = documentDescriptions != null && i < documentDescriptions.Count ? documentDescriptions[i] : null,
                        UploadDate = DateTime.Now
                    };

                    _context.PatientDocuments.Add(document);
                }
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Guarda los documentos importados de turnos como documentos del paciente
        /// Los archivos ya fueron copiados previamente, solo necesitamos crear los registros en BD
        /// </summary>
        private async Task SaveImportedDocuments(int patientId, List<string> importedPaths, List<string>? importedTypes, List<string>? importedNames)
        {
            for (int i = 0; i < importedPaths.Count; i++)
            {
                var filePath = importedPaths[i];
                var fullPath = Path.Combine(_environment.WebRootPath, filePath.TrimStart('/'));
                
                if (!System.IO.File.Exists(fullPath))
                {
                    _logger.LogWarning("Archivo importado no encontrado: {Path}", fullPath);
                    continue;
                }

                try
                {
                    var fileInfo = new FileInfo(fullPath);
                    var fileName = importedNames != null && i < importedNames.Count 
                        ? importedNames[i] 
                        : Path.GetFileName(filePath);
                    
                    // Determinar ContentType basado en extensión
                    var extension = Path.GetExtension(filePath).ToLower();
                    string contentType = extension switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".pdf" => "application/pdf",
                        _ => "application/octet-stream"
                    };

                    var document = new PatientDocument
                    {
                        PatientId = patientId,
                        DocumentType = importedTypes != null && i < importedTypes.Count ? importedTypes[i] : "Importado de Turno",
                        FileName = fileName,
                        FilePath = filePath,
                        FileSize = fileInfo.Length,
                        ContentType = contentType,
                        Description = "Documento importado de solicitud de turno",
                        UploadDate = DateTime.Now
                    };

                    _context.PatientDocuments.Add(document);
                    _logger.LogInformation("Documento importado registrado para paciente {PatientId}: {Path}", patientId, filePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error guardando documento importado: {Path}", filePath);
                }
            }

            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Request model para importar documentos de turnos
    /// </summary>
    public class ImportDocumentsRequest
    {
        public List<string> DocumentPaths { get; set; } = new List<string>();
        public List<string>? DocumentTypes { get; set; }
    }

    /// <summary>
    /// Request model para limpiar documentos importados temporalmente
    /// </summary>
    public class CleanupDocumentsRequest
    {
        public List<string> FilePaths { get; set; } = new List<string>();
    }
}
