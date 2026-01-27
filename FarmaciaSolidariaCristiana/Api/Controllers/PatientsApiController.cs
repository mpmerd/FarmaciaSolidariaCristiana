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
                    Notes = d.Description,
                    UploadedAt = d.UploadDate
                })
                .ToList() ?? new List<PatientDocumentDto>();

            return ApiOk(documents);
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
    }
}
