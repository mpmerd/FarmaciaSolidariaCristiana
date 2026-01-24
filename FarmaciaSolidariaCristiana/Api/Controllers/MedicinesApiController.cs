using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Models;
using FarmaciaSolidariaCristiana.Api.Models;
using System.Text.Json;

namespace FarmaciaSolidariaCristiana.Api.Controllers
{
    /// <summary>
    /// API para gestión de medicamentos
    /// </summary>
    [Route("api/medicines")]
    public class MedicinesApiController : ApiBaseController
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MedicinesApiController> _logger;

        public MedicinesApiController(
            ApplicationDbContext context,
            IHttpClientFactory httpClientFactory,
            ILogger<MedicinesApiController> logger)
        {
            _context = context;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene todos los medicamentos
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<List<MedicineDto>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? search = null)
        {
            var query = _context.Medicines.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(m => m.Name.Contains(search) || 
                                        (m.NationalCode != null && m.NationalCode.Contains(search)));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var medicines = await query
                .OrderBy(m => m.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MedicineDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    StockQuantity = m.StockQuantity,
                    Unit = m.Unit,
                    NationalCode = m.NationalCode
                })
                .ToListAsync();

            return ApiOk(new PagedResult<MedicineDto>
            {
                Items = medicines,
                Page = page,
                PageSize = pageSize,
                TotalItems = totalItems,
                TotalPages = totalPages
            });
        }

        /// <summary>
        /// Obtiene un medicamento por ID
        /// </summary>
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<MedicineDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> GetById(int id)
        {
            var medicine = await _context.Medicines
                .Where(m => m.Id == id)
                .Select(m => new MedicineDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    StockQuantity = m.StockQuantity,
                    Unit = m.Unit,
                    NationalCode = m.NationalCode
                })
                .FirstOrDefaultAsync();

            if (medicine == null)
            {
                return ApiError("Medicamento no encontrado", 404);
            }

            return ApiOk(medicine);
        }

        /// <summary>
        /// Crea un nuevo medicamento
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<MedicineDto>), 201)]
        [ProducesResponseType(typeof(ApiResponse<object>), 400)]
        public async Task<IActionResult> Create([FromBody] CreateMedicineDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            var medicine = new Medicine
            {
                Name = model.Name,
                Description = model.Description,
                StockQuantity = model.StockQuantity,
                Unit = model.Unit ?? "comprimidos",
                NationalCode = model.NationalCode
            };

            _context.Medicines.Add(medicine);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Medicamento creado vía API: {Name} (ID: {Id})", medicine.Name, medicine.Id);

            var result = new MedicineDto
            {
                Id = medicine.Id,
                Name = medicine.Name,
                Description = medicine.Description,
                StockQuantity = medicine.StockQuantity,
                Unit = medicine.Unit,
                NationalCode = medicine.NationalCode
            };

            return CreatedAtAction(nameof(GetById), new { id = medicine.Id }, 
                new ApiResponse<MedicineDto>
                {
                    Success = true,
                    Message = "Medicamento creado exitosamente",
                    Data = result
                });
        }

        /// <summary>
        /// Actualiza un medicamento existente
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<MedicineDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateMedicineDto model)
        {
            if (!ModelState.IsValid)
            {
                return ApiError("Datos inválidos");
            }

            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
            {
                return ApiError("Medicamento no encontrado", 404);
            }

            medicine.Name = model.Name;
            medicine.Description = model.Description;
            medicine.StockQuantity = model.StockQuantity;
            medicine.Unit = model.Unit ?? medicine.Unit;
            medicine.NationalCode = model.NationalCode;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Medicamento actualizado vía API: {Name} (ID: {Id})", medicine.Name, medicine.Id);

            return ApiOk(new MedicineDto
            {
                Id = medicine.Id,
                Name = medicine.Name,
                Description = medicine.Description,
                StockQuantity = medicine.StockQuantity,
                Unit = medicine.Unit,
                NationalCode = medicine.NationalCode
            }, "Medicamento actualizado exitosamente");
        }

        /// <summary>
        /// Elimina un medicamento
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<bool>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> Delete(int id)
        {
            var medicine = await _context.Medicines.FindAsync(id);
            if (medicine == null)
            {
                return ApiError("Medicamento no encontrado", 404);
            }

            // Verificar si tiene entregas o donaciones asociadas
            var hasDeliveries = await _context.Deliveries.AnyAsync(d => d.MedicineId == id);
            var hasDonations = await _context.Donations.AnyAsync(d => d.MedicineId == id);

            if (hasDeliveries || hasDonations)
            {
                return ApiError("No se puede eliminar el medicamento porque tiene entregas o donaciones asociadas");
            }

            _context.Medicines.Remove(medicine);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Medicamento eliminado vía API: {Name} (ID: {Id})", medicine.Name, id);

            return ApiOk(true, "Medicamento eliminado exitosamente");
        }

        /// <summary>
        /// Obtiene medicamentos con stock disponible
        /// </summary>
        [HttpGet("available")]
        [Authorize(Roles = "Admin,Farmaceutico,Viewer,ViewerPublic")]
        [ProducesResponseType(typeof(ApiResponse<List<MedicineDto>>), 200)]
        public async Task<IActionResult> GetAvailable()
        {
            var medicines = await _context.Medicines
                .Where(m => m.StockQuantity > 0)
                .OrderBy(m => m.Name)
                .Select(m => new MedicineDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Description = m.Description,
                    StockQuantity = m.StockQuantity,
                    Unit = m.Unit,
                    NationalCode = m.NationalCode
                })
                .ToListAsync();

            return ApiOk(medicines);
        }

        /// <summary>
        /// Busca información de medicamento en la API CIMA (Agencia Española de Medicamentos)
        /// </summary>
        /// <param name="cn">Código Nacional del medicamento</param>
        [HttpGet("cima/{cn}")]
        [Authorize(Roles = "Admin,Farmaceutico")]
        [ProducesResponseType(typeof(ApiResponse<CimaSearchResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<object>), 404)]
        public async Task<IActionResult> SearchCima(string cn)
        {
            if (string.IsNullOrEmpty(cn))
            {
                return ApiError("Código Nacional no proporcionado");
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient("CimaApi");
                
                _logger.LogInformation("API: Calling CIMA API for CN: {CN}", cn);
                
                var response = await httpClient.GetAsync($"cima/rest/medicamento?cn={cn}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();

                    if (string.IsNullOrEmpty(jsonString))
                    {
                        return ApiError("La API CIMA no devolvió datos", 404);
                    }

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

                                return ApiOk(new CimaSearchResultDto
                                {
                                    NationalCode = cn,
                                    Name = name ?? "",
                                    Description = description
                                }, "Medicamento encontrado en CIMA");
                            }
                        }
                    }
                }

                return ApiError("No se encontró información para este Código Nacional en CIMA", 404);
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "HTTP error calling CIMA API for CN: {CN}", cn);
                return ApiError($"Error de conexión con CIMA API: {httpEx.Message}", 503);
            }
            catch (TaskCanceledException)
            {
                _logger.LogError("Timeout calling CIMA API for CN: {CN}", cn);
                return ApiError("Timeout al conectar con CIMA API", 504);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling CIMA API for CN: {CN}", cn);
                return ApiError($"Error al conectar con CIMA API: {ex.Message}", 500);
            }
        }
    }
}
