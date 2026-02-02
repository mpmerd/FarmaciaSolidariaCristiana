using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Implementación del servicio de API
/// </summary>
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiService(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    private async Task SetAuthHeaderAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
        }
    }

    private async Task<ApiResponse<T>> GetAsync<T>(string endpoint)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync(endpoint);
            return await ProcessResponseAsync<T>(response);
        }
        catch (HttpRequestException)
        {
            return ErrorResponse<T>(Constants.ErrorConexion);
        }
        catch (Exception ex)
        {
            return ErrorResponse<T>(ex.Message);
        }
    }

    private async Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync(endpoint, data);
            return await ProcessResponseAsync<T>(response);
        }
        catch (HttpRequestException)
        {
            return ErrorResponse<T>(Constants.ErrorConexion);
        }
        catch (Exception ex)
        {
            return ErrorResponse<T>(ex.Message);
        }
    }

    private async Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync(endpoint, data);
            return await ProcessResponseAsync<T>(response);
        }
        catch (HttpRequestException)
        {
            return ErrorResponse<T>(Constants.ErrorConexion);
        }
        catch (Exception ex)
        {
            return ErrorResponse<T>(ex.Message);
        }
    }

    private async Task<ApiResponse<bool>> DeleteAsync(string endpoint)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.DeleteAsync(endpoint);
            
            if (response.IsSuccessStatusCode)
            {
                return new ApiResponse<bool> { Success = true, Data = true };
            }
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<bool>>(content, _jsonOptions);
            return result ?? ErrorResponse<bool>("Error al procesar respuesta");
        }
        catch (HttpRequestException)
        {
            return ErrorResponse<bool>(Constants.ErrorConexion);
        }
        catch (Exception ex)
        {
            return ErrorResponse<bool>(ex.Message);
        }
    }

    private async Task<ApiResponse<T>> ProcessResponseAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        
        Console.WriteLine($"[API] ProcessResponse: Status={response.StatusCode}, ContentLength={content?.Length ?? 0}");
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ErrorResponse<T>(Constants.SesionExpirada);
        }

        // Si la respuesta está vacía
        if (string.IsNullOrWhiteSpace(content))
        {
            Console.WriteLine($"[API] WARNING: Empty response!");
            if (response.IsSuccessStatusCode)
            {
                // Si fue exitoso pero vacío, podría ser un 204 No Content
                return ErrorResponse<T>("El servidor no devolvió datos");
            }
            return ErrorResponse<T>($"Error del servidor: {response.StatusCode}");
        }
        
        // Detectar si la respuesta es HTML (redirect de autenticación)
        if (content.TrimStart().StartsWith("<"))
        {
            Console.WriteLine($"[API] WARNING: Received HTML instead of JSON!");
            return ErrorResponse<T>("Error de autenticación - se recibió HTML en lugar de JSON");
        }

        try
        {
            var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
            Console.WriteLine($"[API] Deserialized: Success={result?.Success}, Message={result?.Message}");
            return result ?? ErrorResponse<T>("Error al procesar respuesta");
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"[API] JSON Error: {ex.Message}");
            Console.WriteLine($"[API] Content preview: {content.Substring(0, Math.Min(500, content.Length))}");
            
            // Intentar extraer el mensaje de error del servidor aunque falle la deserialización del tipo T
            try
            {
                var errorResponse = JsonSerializer.Deserialize<ApiResponse<object>>(content, _jsonOptions);
                if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Message))
                {
                    Console.WriteLine($"[API] Extracted error message: {errorResponse.Message}");
                    return ErrorResponse<T>(errorResponse.Message);
                }
            }
            catch { /* Ignorar si tampoco funciona */ }
            
            return ErrorResponse<T>($"Error del servidor. Código: {(int)response.StatusCode}");
        }
    }

    /// <summary>
    /// Procesa respuestas paginadas y extrae los items
    /// </summary>
    private async Task<ApiResponse<List<T>>> GetPagedAsync<T>(string endpoint)
    {
        try
        {
            await SetAuthHeaderAsync();
            Console.WriteLine($"[API] GetPagedAsync<{typeof(T).Name}> -> {endpoint}");
            
            var response = await _httpClient.GetAsync(endpoint);
            var content = await response.Content.ReadAsStringAsync();
            
            Console.WriteLine($"[API] Response status: {response.StatusCode}");
            Console.WriteLine($"[API] Content length: {content?.Length ?? 0}");
            
            // Detectar HTML
            if (content.TrimStart().StartsWith("<"))
            {
                Console.WriteLine($"[API] WARNING: Received HTML instead of JSON!");
                return ErrorResponse<List<T>>("Error de autenticación - se recibió HTML");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return ErrorResponse<List<T>>(Constants.SesionExpirada);
            }

            // Intentar deserializar como PagedResult
            try
            {
                var pagedResult = JsonSerializer.Deserialize<ApiResponse<PagedResult<T>>>(content, _jsonOptions);
                Console.WriteLine($"[API] PagedResult: Success={pagedResult?.Success}, Items={pagedResult?.Data?.Items?.Count ?? 0}");
                
                if (pagedResult?.Success == true && pagedResult.Data != null)
                {
                    return new ApiResponse<List<T>> 
                    { 
                        Success = true, 
                        Data = pagedResult.Data.Items,
                        Message = pagedResult.Message 
                    };
                }
            }
            catch (JsonException jsonEx)
            {
                Console.WriteLine($"[API] PagedResult parse error: {jsonEx.Message}");
            }

            // Si falla, intentar como lista directa
            Console.WriteLine($"[API] Trying list deserialization...");
            var listResult = JsonSerializer.Deserialize<ApiResponse<List<T>>>(content, _jsonOptions);
            Console.WriteLine($"[API] List result: Success={listResult?.Success}, Items={listResult?.Data?.Count ?? 0}");
            return listResult ?? ErrorResponse<List<T>>("Error al procesar respuesta");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"[API] HttpRequestException: {ex.Message}");
            return ErrorResponse<List<T>>(Constants.ErrorConexion);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API] Exception: {ex.Message}");
            return ErrorResponse<List<T>>(ex.Message);
        }
    }

    private static ApiResponse<T> ErrorResponse<T>(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message };
    }

    // === TURNOS ===
    
    public Task<ApiResponse<List<Turno>>> GetTurnosAsync()
        => GetPagedAsync<Turno>("/api/turnos?pageSize=100");

    public Task<ApiResponse<List<Turno>>> GetMisTurnosAsync()
        => GetAsync<List<Turno>>("/api/turnos/my");

    public Task<ApiResponse<Turno>> GetTurnoAsync(int id)
        => GetAsync<Turno>($"/api/turnos/{id}");

    public Task<ApiResponse<Turno>> CrearTurnoAsync(CrearTurnoRequest request)
        => PostAsync<Turno>("/api/turnos", request);

    public async Task<ApiResponse<Turno>> CrearTurnoMobileAsync(ViewModels.CrearTurnoMobileRequest request)
    {
        // Convertir al formato que espera la API
        var apiRequest = new
        {
            DocumentoIdentidad = request.DocumentoIdentidad,
            TipoSolicitud = request.TipoSolicitud,
            Notas = request.Notas,
            Items = request.Items.Select(i => new { Id = i.Id, Cantidad = i.Cantidad }).ToList()
        };
        return await PostAsync<Turno>("/api/turnos", apiRequest);
    }

    public async Task<ApiResponse<TurnoDocumentoResponse>> SubirDocumentoTurnoAsync(
        int turnoId, string fileName, string documentType, byte[] fileBytes, string? description)
    {
        try
        {
            await SetAuthHeaderAsync();

            using var content = new MultipartFormDataContent();
            
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) 
                    ? "application/pdf" 
                    : "image/jpeg");
            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(documentType), "documentType");
            
            if (!string.IsNullOrEmpty(description))
            {
                content.Add(new StringContent(description), "description");
            }

            var response = await _httpClient.PostAsync($"/api/turnos/{turnoId}/documents", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[API] SubirDocumentoTurno status: {response.StatusCode}");
            Console.WriteLine($"[API] SubirDocumentoTurno response: {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<TurnoDocumentoResponse>>(responseContent, _jsonOptions);
                return result ?? ErrorResponse<TurnoDocumentoResponse>("Error al procesar respuesta");
            }

            return ErrorResponse<TurnoDocumentoResponse>($"Error: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API] SubirDocumentoTurno error: {ex.Message}");
            return ErrorResponse<TurnoDocumentoResponse>(ex.Message);
        }
    }

    public Task<ApiResponse<Turno>> AprobarTurnoAsync(int id, string? comentarios = null)
        => PostAsync<Turno>($"/api/turnos/{id}/approve", new { Comentarios = comentarios });

    public Task<ApiResponse<Turno>> RechazarTurnoAsync(int id, string motivo)
        => PostAsync<Turno>($"/api/turnos/{id}/reject", new { motivo });

    public Task<ApiResponse<Turno>> ReprogramarTurnoAsync(int id, DateTime nuevaFecha, string? motivo)
        => PostAsync<Turno>($"/api/turnos/{id}/reschedule", new { nuevaFecha, motivo });

    public Task<ApiResponse<Turno>> CancelarTurnoAsync(int id, string motivo)
        => PostAsync<Turno>($"/api/turnos/{id}/cancel", new { motivo });

    public Task<ApiResponse<CanCancelTurnoResponse>> PuedeCancelarTurnoAsync(int id)
        => GetAsync<CanCancelTurnoResponse>($"/api/turnos/{id}/can-cancel");

    public async Task<byte[]?> DescargarTurnoPdfAsync(int id)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[ApiService] Descargando PDF del turno {id}...");
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"/api/turnos/{id}/pdf");
            
            System.Diagnostics.Debug.WriteLine($"[ApiService] Respuesta PDF: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                System.Diagnostics.Debug.WriteLine($"[ApiService] PDF descargado: {bytes.Length} bytes");
                return bytes;
            }
            
            // Log del error del servidor
            var errorContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[ApiService] Error descargando PDF: {response.StatusCode} - {errorContent}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiService] Excepción descargando PDF: {ex.Message}");
            return null;
        }
    }

    // === MEDICAMENTOS ===

    public Task<ApiResponse<List<Medicine>>> GetMedicamentosAsync()
        => GetPagedAsync<Medicine>("/api/medicines?pageSize=500");

    public Task<ApiResponse<Medicine>> GetMedicamentoAsync(int id)
        => GetAsync<Medicine>($"/api/medicines/{id}");

    public Task<ApiResponse<Medicine>> CrearMedicamentoAsync(Medicine medicamento)
        => PostAsync<Medicine>("/api/medicines", medicamento);

    public Task<ApiResponse<Medicine>> ActualizarMedicamentoAsync(Medicine medicamento)
        => PutAsync<Medicine>($"/api/medicines/{medicamento.Id}", medicamento);

    public Task<ApiResponse<bool>> EliminarMedicamentoAsync(int id)
        => DeleteAsync($"/api/medicines/{id}");

    // === INSUMOS ===

    public Task<ApiResponse<List<Supply>>> GetInsumosAsync()
        => GetPagedAsync<Supply>("/api/supplies?pageSize=500");

    public Task<ApiResponse<Supply>> GetInsumoAsync(int id)
        => GetAsync<Supply>($"/api/supplies/{id}");

    public Task<ApiResponse<Supply>> CrearInsumoAsync(Supply insumo)
        => PostAsync<Supply>("/api/supplies", insumo);

    public Task<ApiResponse<Supply>> ActualizarInsumoAsync(Supply insumo)
        => PutAsync<Supply>($"/api/supplies/{insumo.Id}", insumo);

    public Task<ApiResponse<bool>> EliminarInsumoAsync(int id)
        => DeleteAsync($"/api/supplies/{id}");

    // === DONACIONES ===

    public Task<ApiResponse<List<Donation>>> GetDonacionesAsync()
        => GetPagedAsync<Donation>("/api/donations?pageSize=500");

    public Task<ApiResponse<Donation>> GetDonacionAsync(int id)
        => GetAsync<Donation>($"/api/donations/{id}");

    public Task<ApiResponse<Donation>> CrearDonacionAsync(Donation donacion)
        => PostAsync<Donation>("/api/donations", donacion);

    public Task<ApiResponse<Donation>> ActualizarDonacionAsync(Donation donacion)
        => PutAsync<Donation>($"/api/donations/{donacion.Id}", donacion);

    public Task<ApiResponse<bool>> EliminarDonacionAsync(int id)
        => DeleteAsync($"/api/donations/{id}");

    // === ENTREGAS ===

    public Task<ApiResponse<List<Delivery>>> GetEntregasAsync()
        => GetPagedAsync<Delivery>("/api/deliveries?pageSize=500");

    public Task<ApiResponse<Delivery>> GetEntregaAsync(int id)
        => GetAsync<Delivery>($"/api/deliveries/{id}");

    public Task<ApiResponse<Delivery>> CrearEntregaAsync(Delivery entrega)
        => PostAsync<Delivery>("/api/deliveries", entrega);

    public Task<ApiResponse<bool>> EliminarEntregaAsync(int id)
        => DeleteAsync($"/api/deliveries/{id}");

    // === PACIENTES ===

    public Task<ApiResponse<List<Patient>>> GetPacientesAsync()
        => GetPagedAsync<Patient>("/api/patients?pageSize=500");

    public Task<ApiResponse<Patient>> GetPacienteAsync(int id)
        => GetAsync<Patient>($"/api/patients/{id}");

    public Task<ApiResponse<Patient>> CrearPacienteAsync(Patient paciente)
        => PostAsync<Patient>("/api/patients", paciente);

    public Task<ApiResponse<Patient>> ActualizarPacienteAsync(Patient paciente)
        => PutAsync<Patient>($"/api/patients/{paciente.Id}", paciente);

    public Task<ApiResponse<bool>> DeletePacienteAsync(int id)
        => DeleteAsync($"/api/patients/{id}");

    public Task<ApiResponse<List<PatientDocument>>> GetDocumentosPacienteAsync(int patientId)
        => GetAsync<List<PatientDocument>>($"/api/patients/{patientId}/documents");

    public async Task<ApiResponse<PatientDocument>> SubirDocumentoPacienteAsync(
        int patientId, string fileName, string documentType, byte[] fileBytes, string? notes)
    {
        try
        {
            await SetAuthHeaderAsync();

            using var content = new MultipartFormDataContent();
            
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
                fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) 
                    ? "application/pdf" 
                    : "image/jpeg");
            
            content.Add(fileContent, "file", fileName);
            content.Add(new StringContent(documentType), "documentType");
            if (!string.IsNullOrEmpty(notes))
            {
                content.Add(new StringContent(notes), "notes");
            }

            var response = await _httpClient.PostAsync($"/api/patients/{patientId}/documents", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<PatientDocument>>(responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result ?? new ApiResponse<PatientDocument> { Success = false, Message = "Error deserializando respuesta" };
            }
            
            return new ApiResponse<PatientDocument> 
            { 
                Success = false, 
                Message = $"Error: {response.StatusCode}" 
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PatientDocument> 
            { 
                Success = false, 
                Message = ex.Message 
            };
        }
    }

    // === PATROCINADORES ===

    public Task<ApiResponse<List<Sponsor>>> GetPatrocinadoresAsync()
        => GetAsync<List<Sponsor>>("/api/sponsors");

    // === FECHAS BLOQUEADAS ===

    public Task<ApiResponse<List<FechaBloqueadaDto>>> GetFechasBloqueadasAsync()
        => GetAsync<List<FechaBloqueadaDto>>("/api/fechasbloqueadas");

    public Task<ApiResponse<FechaBloqueadaDto>> CreateFechaBloqueadaAsync(object request)
        => PostAsync<FechaBloqueadaDto>("/api/fechasbloqueadas", request);

    public Task<ApiResponse<bool>> DeleteFechaBloqueadaAsync(int id)
        => DeleteAsync($"/api/fechasbloqueadas/{id}");

    // === DELETE ALIASES ===

    public Task<ApiResponse<bool>> DeleteInsumoAsync(int id)
        => EliminarInsumoAsync(id);

    public Task<ApiResponse<bool>> DeleteDonacionAsync(int id)
        => EliminarDonacionAsync(id);

    public Task<ApiResponse<bool>> DeleteEntregaAsync(int id)
        => EliminarEntregaAsync(id);

    // === REPORTES ===

    public async Task<byte[]?> DescargarReporteAsync(string tipoReporte, DateTime? fechaInicio = null, DateTime? fechaFin = null)
    {
        try
        {
            await SetAuthHeaderAsync();
            
            var url = $"/api/reports/{tipoReporte}";
            
            // Construir el objeto de request según el tipo de reporte
            object requestBody;
            if (tipoReporte == "monthly" && fechaInicio.HasValue)
            {
                requestBody = new
                {
                    Year = fechaInicio.Value.Year,
                    Month = fechaInicio.Value.Month
                };
            }
            else
            {
                requestBody = new
                {
                    StartDate = fechaInicio,
                    EndDate = fechaFin
                };
            }
            
            var response = await _httpClient.PostAsJsonAsync(url, requestBody);
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Reporte response: {content.Substring(0, Math.Min(500, content.Length))}...");
                
                var apiResponse = JsonSerializer.Deserialize<ApiResponseReport>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (apiResponse?.Success == true && apiResponse.Data?.PdfBase64 != null)
                {
                    return Convert.FromBase64String(apiResponse.Data.PdfBase64);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"Reporte API error: Success={apiResponse?.Success}, Data={apiResponse?.Data != null}, Message={apiResponse?.Message}");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"Reporte HTTP error {response.StatusCode}: {errorContent}");
            }
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error descargando reporte: {ex.Message}");
            return null;
        }
    }

    // === AUTENTICACIÓN ===

    public async Task<ApiResponse<RegistrationStatusDto>> GetRegistrationStatusAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/auth/registration-status");
            return await ProcessResponseAsync<RegistrationStatusDto>(response);
        }
        catch (Exception ex)
        {
            return ErrorResponse<RegistrationStatusDto>($"Error de conexión: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> RegisterAsync(RegisterRequest request)
    {
        try
        {
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/auth/register", content);
            return await ProcessResponseAsync<bool>(response);
        }
        catch (Exception ex)
        {
            return ErrorResponse<bool>($"Error de conexión: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> ForgotPasswordAsync(string emailOrUserName)
    {
        try
        {
            var requestObj = new { EmailOrUserName = emailOrUserName };
            var json = JsonSerializer.Serialize(requestObj, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/auth/forgot-password", content);
            return await ProcessResponseAsync<bool>(response);
        }
        catch (Exception ex)
        {
            return ErrorResponse<bool>($"Error de conexión: {ex.Message}");
        }
    }

    // === DIAGNÓSTICO ===

    public async Task<bool> CheckApiHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/diagnostics/ping");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    // Clases auxiliares para deserializar respuesta de reportes
    private class ApiResponseReport
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public ReportResultData? Data { get; set; }
    }

    private class ReportResultData
    {
        public string? PdfBase64 { get; set; }
        public string? FileName { get; set; }
        public string? ContentType { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
