using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Api.Models;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// API para gestión de pacientes
    /// </summary>
    [Route("api/patients")]
    public class PatientsApiController : ApiBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PatientsApiController> _logger;

        public PatientsApiController(
            ApplicationDbContext context,
            ILogger<PatientsApiController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los pacientes activos con filtros opcionales
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<PatientDto>>), 200)]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20, 
            [FromQuery] string? search = null,
            [FromQuery] bool includeInactive = false)
        {
            var query = _context.Patients.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(p => p.IsActive);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(p => 
                    p.FullName.Contains(search) ||
                    p.IdentificationDocument.Contains(search) ||
                    (p.Municipality != null && p.Municipality.Contains(search)));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var patients = await query
                .OrderByDescending(p => p.RegistrationDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PatientDto
                {
                    Id = p.Id,
                    IdentificationDocument = p.IdentificationDocument,
                    FullName = p.FullName,
                    Age = p.Age,
                    Gender = p.Gender,
                    Address = p.Address,
                    Phone = p.Phone,
                    Municipality = p.Municipality,
                    Province = p.Province,
                    MainDiagnosis = p.MainDiagnosis,
                    KnownAllergies = p.KnownAllergies,
                    IsActive = p.IsActive,
                    RegistrationDate = p.RegistrationDate,
                    DeliveriesCount = p.Deliveries != null ? p.Deliveries.Count : 0
                })
                .ToListAsync();

            return ApiOk(new PagedResult<PatientDto>
            {
                Items = patients,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Obtiene un paciente por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer")]
        [ProducesResponseType(typeof(ApiResponse<PatientDetailDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.Deliveries)
                    .ThenInclude(d => d.Medicine)
                .Include(p => p.Documents)
                .Where(p => p.Id == id)
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return ApiError("Paciente no encontrado", 404);
            }

            var result = new PatientDetailDto
            {
                Id = patient.Id,
                IdentificationDocument = patient.IdentificationDocument,
                FullName = patient.FullName,
                Age = patient.Age,
                Gender = patient.Gender,
                Address = patient.Address,
                Phone = patient.Phone,
                Municipality = patient.Municipality,
                Province = patient.Province,
                MainDiagnosis = patient.MainDiagnosis,
                AssociatedPathologies = patient.AssociatedPathologies,
                KnownAllergies = patient.KnownAllergies,
                CurrentTreatments = patient.CurrentTreatments,
                BloodPressureSystolic = patient.BloodPressureSystolic,
                BloodPressureDiastolic = patient.BloodPressureDiastolic,
                Weight = patient.Weight,
                Height = patient.Height,
                Observations = patient.Observations,
                IsActive = patient.IsActive,
                RegistrationDate = patient.RegistrationDate,
                RecentDeliveries = patient.Deliveries
                    .OrderByDescending(d => d.DeliveryDate)
                    .Take(10)
                    .Select(d => new PatientDeliveryDto
                    {
                        Id = d.Id,
                        MedicineName = d.Medicine?.Name,
                        Quantity = d.Quantity,
                        DeliveryDate = d.DeliveryDate
                    })
                    .ToList(),
                DocumentsCount = patient.Documents?.Count ?? 0
            };

            return ApiOk(result);
        }

        /// <summary>
        /// Busca un paciente por número de identificación
        /// </summary>
        [HttpGet("by-identification/{identification}")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetByIdentification(string identification)
        {
            var patient = await _context.Patients
                .Include(p => p.Deliveries)
                .Where(p => p.IdentificationDocument == identification && p.IsActive)
                .Select(p => new PatientDto
                {
                    Id = p.Id,
                    IdentificationDocument = p.IdentificationDocument,
                    FullName = p.FullName,
                    Age = p.Age,
                    Gender = p.Gender,
                    Address = p.Address,
                    Phone = p.Phone,
                    Municipality = p.Municipality,
                    Province = p.Province,
                    MainDiagnosis = p.MainDiagnosis,
                    KnownAllergies = p.KnownAllergies,
                    IsActive = p.IsActive,
                    RegistrationDate = p.RegistrationDate,
                    DeliveriesCount = p.Deliveries != null ? p.Deliveries.Count : 0
                })
                .FirstOrDefaultAsync();

            if (patient == null)
            {
                return ApiError("Paciente no encontrado", 404);
            }

            return ApiOk(patient);
        }

        /// <summary>
        /// Obtiene los documentos médicos de un paciente
        /// </summary>
        [HttpGet("{id}/documents")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<List<PatientDocumentDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetDocuments(int id)
        {
            var patient = await _context.Patients
                .Include(p => p.Documents)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null)
            {
                return ApiError("Paciente no encontrado", 404);
            }

            var documents = patient.Documents?
                .OrderByDescending(d => d.UploadDate)
                .Select(d => new PatientDocumentDto
                {
                    Id = d.Id,
                    PatientId = d.PatientId,
                    DocumentType = d.DocumentType,
                    FileName = d.FileName,
                    FilePath = d.FilePath,
                    ContentType = d.ContentType,
                    FileSize = d.FileSize,
                    Notes = d.Description,
                    UploadedAt = d.UploadDate
                })
                .ToList() ?? new List<PatientDocumentDto>();

            return ApiOk(documents);
        }

        /// <summary>
        /// Sube un nuevo documento médico para un paciente
        /// </summary>
        [HttpPost("{patientId}/documents")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(20 * 1024 * 1024)] // 20MB max
        [ProducesResponseType(typeof(ApiResponse<PatientDocumentDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> UploadDocument(
            int patientId,
            [FromForm] IFormFile document,
            [FromForm] string documentType,
            [FromForm] string? notes = null)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return ApiError("Paciente no encontrado", 404);
            }

            if (document == null || document.Length == 0)
            {
                return ApiError("No se proporcionó ningún archivo");
            }

            // Validar tipo de archivo
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".webp" };
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                return ApiError($"Tipo de archivo no permitido. Solo se permiten: {string.Join(", ", allowedExtensions)}");
            }

            // Validar tipo de contenido
            var allowedContentTypes = new[] { 
                "image/jpeg", "image/png", "image/gif", "image/webp",
                "application/pdf"
            };
            if (!allowedContentTypes.Contains(document.ContentType?.ToLowerInvariant()))
            {
                return ApiError($"Tipo de contenido no permitido: {document.ContentType}");
            }

            try
            {
                // Crear directorio si no existe
                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "patient-documents");
                Directory.CreateDirectory(uploadsPath);

                // Generar nombre único
                var fileName = $"{patientId}_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid():N}{extension}";
                var filePath = Path.Combine(uploadsPath, fileName);
                var relativePath = $"/uploads/patient-documents/{fileName}";  // Con / inicial para ruta absoluta

                // Guardar archivo
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await document.CopyToAsync(stream);
                }

                // Crear registro en la base de datos
                var patientDocument = new PatientDocument
                {
                    PatientId = patientId,
                    DocumentType = documentType,
                    FileName = document.FileName,
                    FilePath = relativePath,
                    FileSize = document.Length,
                    ContentType = document.ContentType,
                    Description = notes,
                    UploadDate = DateTime.Now
                };

                _context.PatientDocuments.Add(patientDocument);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Documento subido para paciente {PatientId}: {FileName} ({ContentType})", 
                    patientId, document.FileName, document.ContentType);

                return ApiOk(new PatientDocumentDto
                {
                    Id = patientDocument.Id,
                    PatientId = patientDocument.PatientId,
                    DocumentType = patientDocument.DocumentType,
                    FileName = patientDocument.FileName,
                    FilePath = patientDocument.FilePath,
                    ContentType = patientDocument.ContentType,
                    FileSize = patientDocument.FileSize,
                    Notes = patientDocument.Description,
                    UploadedAt = patientDocument.UploadDate
                }, "Documento subido exitosamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error subiendo documento para paciente {PatientId}", patientId);
                return ApiError($"Error al subir el documento: {ex.Message}");
            }
        }

        /// <summary>
        /// Crea un nuevo paciente
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Create([FromBody] CreatePatientDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            // Verificar si ya existe un paciente con ese documento
            var existing = await _context.Patients
                .AnyAsync(p => p.IdentificationDocument == model.IdentificationDocument);
            
            if (existing)
            {
                return ApiError("Ya existe un paciente con ese número de identificación");
            }

            var patient = new Patient
            {
                IdentificationDocument = model.IdentificationDocument,
                FullName = model.FullName,
                Age = model.Age,
                Gender = model.Gender,
                Address = model.Address,
                Phone = model.Phone,
                Municipality = model.Municipality,
                Province = model.Province,
                MainDiagnosis = model.MainDiagnosis,
                AssociatedPathologies = model.AssociatedPathologies,
                KnownAllergies = model.KnownAllergies,
                CurrentTreatments = model.CurrentTreatments,
                Observations = model.Observations,
                IsActive = true,
                RegistrationDate = DateTime.Now
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Paciente creado vía API: {Name} (ID: {Id})", patient.FullName, patient.Id);

            var result = new PatientDto
            {
                Id = patient.Id,
                IdentificationDocument = patient.IdentificationDocument,
                FullName = patient.FullName,
                Age = patient.Age,
                Gender = patient.Gender,
                Address = patient.Address,
                Phone = patient.Phone,
                Municipality = patient.Municipality,
                Province = patient.Province,
                MainDiagnosis = patient.MainDiagnosis,
                IsActive = patient.IsActive,
                RegistrationDate = patient.RegistrationDate
            };

            return CreatedAtAction(nameof(GetById), new { id = patient.Id },
                new ApiResponse<PatientDto>
                {
                    Success = true,
                    Message = "Paciente registrado exitosamente",
                    Data = result
                });
        }

        /// <summary>
        /// Actualiza un paciente existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<PatientDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdatePatientDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return ApiError("Paciente no encontrado", 404);
            }

            patient.FullName = model.FullName;
            patient.Age = model.Age;
            patient.Gender = model.Gender;
            patient.Address = model.Address;
            patient.Phone = model.Phone;
            patient.Municipality = model.Municipality;
            patient.Province = model.Province;
            patient.MainDiagnosis = model.MainDiagnosis;
            patient.AssociatedPathologies = model.AssociatedPathologies;
            patient.KnownAllergies = model.KnownAllergies;
            patient.CurrentTreatments = model.CurrentTreatments;
            patient.BloodPressureSystolic = model.BloodPressureSystolic;
            patient.BloodPressureDiastolic = model.BloodPressureDiastolic;
            patient.Weight = model.Weight;
            patient.Height = model.Height;
            patient.Observations = model.Observations;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Paciente actualizado vía API: {Name} (ID: {Id})", patient.FullName, patient.Id);

            return ApiOk(new PatientDto
            {
                Id = patient.Id,
                IdentificationDocument = patient.IdentificationDocument,
                FullName = patient.FullName,
                Age = patient.Age,
                Gender = patient.Gender,
                IsActive = patient.IsActive,
                RegistrationDate = patient.RegistrationDate
            }, "Paciente actualizado exitosamente");
        }

        /// <summary>
        /// Desactiva un paciente (soft delete)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Deactivate(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return ApiError("Paciente no encontrado", 404);
            }

            patient.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Paciente desactivado vía API: {Name} (ID: {Id})", patient.FullName, id);

            return ApiOk(true, "Paciente desactivado exitosamente");
        }

        /// <summary>
        /// Reactiva un paciente
        /// </summary>
        [HttpPost("{id}/reactivate")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Reactivate(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return ApiError("Paciente no encontrado", 404);
            }

            patient.IsActive = true;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Paciente reactivado vía API: {Name} (ID: {Id})", patient.FullName, id);

            return ApiOk(true, "Paciente reactivado exitosamente");
        }

        /// <summary>
        /// Obtiene estadísticas de pacientes
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<PatientStatsDto>), 200)]
        public async Task<IActionResult> GetStats()
        {
            var stats = new PatientStatsDto
            {
                TotalPatients = await _context.Patients.CountAsync(p => p.IsActive),
                TotalInactive = await _context.Patients.CountAsync(p => !p.IsActive),
                NewThisMonth = await _context.Patients
                    .CountAsync(p => p.RegistrationDate.Month == DateTime.Now.Month && 
                                    p.RegistrationDate.Year == DateTime.Now.Year)
            };

            return ApiOk(stats);
        }

        /// <summary>
        /// Busca documentos de turnos APROBADOS por número de identificación del paciente.
        /// Útil para importar documentos médicos al crear/editar ficha de paciente.
        /// </summary>
        [HttpGet("turno-documents/{identification}")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<TurnoDocumentsSearchResultDto>), 200)]
        public async Task<IActionResult> GetTurnoDocumentsByIdentification(string identification)
        {
            if (string.IsNullOrWhiteSpace(identification))
            {
                return ApiError("Número de identificación requerido");
            }

            // Calcular hash del documento de identidad
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(identification));
            var documentHash = Convert.ToBase64String(hashBytes);

            // Buscar turnos APROBADOS con documentos para esta identificación
            var turnosConDocumentos = await _context.Turnos
                .Include(t => t.Documentos)
                .Where(t => t.DocumentoIdentidadHash == documentHash && t.Estado == EstadoTurno.Aprobado)
                .OrderByDescending(t => t.FechaSolicitud)
                .ToListAsync();

            var documentos = new List<TurnoDocumentItemDto>();

            foreach (var turno in turnosConDocumentos)
            {
                // Documentos de la tabla TurnoDocumentos
                if (turno.Documentos != null && turno.Documentos.Any())
                {
                    foreach (var doc in turno.Documentos)
                    {
                        documentos.Add(new TurnoDocumentItemDto
                        {
                            Id = doc.Id,
                            TurnoId = turno.Id,
                            NumeroTurno = turno.NumeroTurno,
                            DocumentType = doc.DocumentType,
                            FileName = doc.FileName,
                            FilePath = doc.FilePath,
                            ContentType = doc.ContentType,
                            FileSize = doc.FileSize,
                            FechaSolicitud = turno.FechaSolicitud,
                            Source = "turno_documento"
                        });
                    }
                }

                // Campos antiguos: RecetaMedicaPath y TarjetonPath
                if (!string.IsNullOrEmpty(turno.RecetaMedicaPath))
                {
                    documentos.Add(new TurnoDocumentItemDto
                    {
                        Id = 0,
                        TurnoId = turno.Id,
                        NumeroTurno = turno.NumeroTurno,
                        DocumentType = "Receta Médica",
                        FileName = Path.GetFileName(turno.RecetaMedicaPath),
                        FilePath = turno.RecetaMedicaPath,
                        ContentType = GetContentType(turno.RecetaMedicaPath),
                        FechaSolicitud = turno.FechaSolicitud,
                        Source = "turno_receta"
                    });
                }

                if (!string.IsNullOrEmpty(turno.TarjetonPath))
                {
                    documentos.Add(new TurnoDocumentItemDto
                    {
                        Id = 0,
                        TurnoId = turno.Id,
                        NumeroTurno = turno.NumeroTurno,
                        DocumentType = "Tarjetón Sanitario",
                        FileName = Path.GetFileName(turno.TarjetonPath),
                        FilePath = turno.TarjetonPath,
                        ContentType = GetContentType(turno.TarjetonPath),
                        FechaSolicitud = turno.FechaSolicitud,
                        Source = "turno_tarjeton"
                    });
                }
            }

            return ApiOk(new TurnoDocumentsSearchResultDto
            {
                Found = documentos.Any(),
                Count = documentos.Count,
                Documents = documentos,
                Message = documentos.Any()
                    ? $"Se encontraron {documentos.Count} documento(s) en turnos aprobados."
                    : "No se encontraron documentos en turnos aprobados para esta identificación."
            });
        }

        /// <summary>
        /// Importa documentos de turnos como documentos de paciente.
        /// </summary>
        [HttpPost("{patientId}/import-turno-documents")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<ImportDocumentsResultDto>), 200)]
        public async Task<IActionResult> ImportTurnoDocuments(int patientId, [FromBody] ImportTurnoDocumentsDto request)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            if (patient == null)
            {
                return ApiError("Paciente no encontrado", 404);
            }

            if (request?.Documents == null || !request.Documents.Any())
            {
                return ApiError("No se especificaron documentos para importar");
            }

            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadFolder = Path.Combine(webRootPath, "uploads", "patient-documents");
            if (!Directory.Exists(uploadFolder))
            {
                Directory.CreateDirectory(uploadFolder);
            }

            var importedDocs = new List<PatientDocumentDto>();
            var errors = new List<string>();

            foreach (var docInfo in request.Documents)
            {
                try
                {
                    // Construir ruta del archivo origen
                    var sourcePath = Path.Combine(webRootPath, docInfo.FilePath.TrimStart('/'));

                    if (!System.IO.File.Exists(sourcePath))
                    {
                        _logger.LogWarning("Archivo no encontrado para importar: {Path}", sourcePath);
                        errors.Add($"Archivo no encontrado: {docInfo.FileName}");
                        continue;
                    }

                    // Generar nuevo nombre para el archivo copiado
                    var fileExtension = Path.GetExtension(docInfo.FilePath).ToLower();
                    var newFileName = $"patient_{patientId}_{Guid.NewGuid()}{fileExtension}";
                    var destPath = Path.Combine(uploadFolder, newFileName);

                    // Copiar el archivo
                    System.IO.File.Copy(sourcePath, destPath);
                    var fileInfo = new FileInfo(destPath);
                    
                    // Determinar ContentType basado en extensión
                    var contentType = fileExtension switch
                    {
                        ".jpg" or ".jpeg" => "image/jpeg",
                        ".png" => "image/png",
                        ".gif" => "image/gif",
                        ".webp" => "image/webp",
                        ".pdf" => "application/pdf",
                        _ => "application/octet-stream"
                    };

                    // Crear registro en base de datos
                    var patientDoc = new PatientDocument
                    {
                        PatientId = patientId,
                        DocumentType = docInfo.DocumentType,
                        FileName = docInfo.FileName,
                        FilePath = $"/uploads/patient-documents/{newFileName}",
                        FileSize = fileInfo.Length,
                        ContentType = contentType,
                        Description = $"Importado de turno #{docInfo.NumeroTurno} ({docInfo.FechaSolicitud:dd/MM/yyyy})",
                        UploadDate = DateTime.Now
                    };

                    _context.PatientDocuments.Add(patientDoc);
                    await _context.SaveChangesAsync();

                    importedDocs.Add(new PatientDocumentDto
                    {
                        Id = patientDoc.Id,
                        PatientId = patientId,
                        DocumentType = patientDoc.DocumentType,
                        FileName = patientDoc.FileName,
                        FilePath = patientDoc.FilePath,
                        ContentType = patientDoc.ContentType,
                        FileSize = patientDoc.FileSize,
                        Notes = patientDoc.Description,
                        UploadedAt = patientDoc.UploadDate
                    });

                    _logger.LogInformation(
                        "Documento importado de turno a paciente: {Source} -> {Dest} (Paciente: {PatientId})",
                        sourcePath, destPath, patientId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importando documento: {Path}", docInfo.FilePath);
                    errors.Add($"Error al importar {docInfo.FileName}: {ex.Message}");
                }
            }

            return ApiOk(new ImportDocumentsResultDto
            {
                Success = importedDocs.Any(),
                ImportedCount = importedDocs.Count,
                ImportedDocuments = importedDocs,
                Errors = errors,
                Message = importedDocs.Any()
                    ? $"Se importaron {importedDocs.Count} documento(s) correctamente."
                    : "No se pudo importar ningún documento."
            });
        }

        /// <summary>
        /// Descarga un documento de paciente (para ver en la app)
        /// </summary>
        [HttpGet("{patientId}/documents/{documentId}/download")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        public async Task<IActionResult> DownloadDocument(int patientId, int documentId)
        {
            var document = await _context.PatientDocuments
                .FirstOrDefaultAsync(d => d.Id == documentId && d.PatientId == patientId);

            if (document == null)
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "Documento no encontrado" });
            }

            var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var filePath = Path.Combine(webRootPath, document.FilePath.TrimStart('/'));

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new ApiResponse<object> { Success = false, Message = "Archivo no encontrado en el servidor" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            var contentType = GetContentType(document.FileName);

            return File(fileBytes, contentType, document.FileName);
        }

        private static string GetContentType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }
    }

    // ========== DTOs para documentos de turnos ==========
    
    public class TurnoDocumentsSearchResultDto
    {
        public bool Found { get; set; }
        public int Count { get; set; }
        public List<TurnoDocumentItemDto> Documents { get; set; } = new();
        public string? Message { get; set; }
    }

    public class TurnoDocumentItemDto
    {
        public int Id { get; set; }
        public int TurnoId { get; set; }
        public int? NumeroTurno { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public string Source { get; set; } = string.Empty;
    }

    public class ImportTurnoDocumentsDto
    {
        public List<TurnoDocumentImportItemDto> Documents { get; set; } = new();
    }

    public class TurnoDocumentImportItemDto
    {
        public int TurnoId { get; set; }
        public int? NumeroTurno { get; set; }
        public string DocumentType { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime FechaSolicitud { get; set; }
    }

    public class ImportDocumentsResultDto
    {
        public bool Success { get; set; }
        public int ImportedCount { get; set; }
        public List<PatientDocumentDto> ImportedDocuments { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public string? Message { get; set; }
    }
}
