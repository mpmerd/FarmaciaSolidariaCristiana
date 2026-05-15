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
    private readonly ICacheService _cache;
    private readonly JsonSerializerOptions _jsonOptions;

    private const string CacheKeyMedicamentos = "/api/medicines";
    private const string CacheKeyInsumos = "/api/supplies";
    private const string CacheKeyTurnos = "/api/turnos";
    private const string CacheKeyMisTurnos = "/api/turnos/my";
    private const string CacheKeyDonaciones = "/api/donations";
    private const string CacheKeyEntregas = "/api/deliveries";
    private const string CacheKeyPacientes = "/api/patients";
    private const string CacheKeyUsuarios = "/api/users";

    public ApiService(HttpClient httpClient, IAuthService authService, ICacheService cache)
    {
        _httpClient = httpClient;
        _authService = authService;
        _cache = cache;
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

    /// <summary>
    /// Maneja la expiración de sesión: hace logout y navega a login
    /// </summary>
    private async Task HandleSessionExpiredAsync()
    {
        try
        {
            Console.WriteLine("[API] Session expired - forcing logout and navigation to login");
            
            // Hacer logout para limpiar credenciales
            await _authService.LogoutAsync();
            
            // Navegar a login en el hilo principal
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Mostrar mensaje al usuario
                if (Shell.Current?.CurrentPage != null)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Sesión Expirada",
                        "Su sesión ha expirado. Por favor, inicie sesión nuevamente.",
                        "OK");
                }
                
                // Navegar a login
                await Shell.Current!.GoToAsync("//LoginPage");
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[API] Error handling session expiration: {ex.Message}");
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
            // Sesión expirada - forzar logout automático y navegar a login
            await HandleSessionExpiredAsync();
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
            // También puede indicar sesión expirada (redirect a login)
            await HandleSessionExpiredAsync();
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
            
            // Detectar HTML (puede indicar sesión expirada - redirect a login)
            if (content?.TrimStart().StartsWith("<") == true)
            {
                Console.WriteLine($"[API] WARNING: Received HTML instead of JSON!");
                await HandleSessionExpiredAsync();
                return ErrorResponse<List<T>>("Error de autenticación - se recibió HTML");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await HandleSessionExpiredAsync();
                return ErrorResponse<List<T>>(Constants.SesionExpirada);
            }

            // Intentar deserializar como PagedResult
            try
            {
                var pagedResult = JsonSerializer.Deserialize<ApiResponse<PagedResult<T>>>(content!, _jsonOptions);
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
            var listResult = JsonSerializer.Deserialize<ApiResponse<List<T>>>(content!, _jsonOptions);
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

    public async Task<ApiResponse<List<Turno>>> GetTurnosAsync()
    {
        if (_cache.TryGet<List<Turno>>(CacheKeyTurnos, out var cached) && cached != null)
            return new ApiResponse<List<Turno>> { Success = true, Data = cached };
        var result = await GetPagedAsync<Turno>("/api/turnos?pageSize=100");
        if (result.Success && result.Data != null)
            _cache.Set(CacheKeyTurnos, result.Data, TimeSpan.FromMinutes(2));
        return result;
    }

    public async Task<ApiResponse<List<Turno>>> GetMisTurnosAsync()
    {
        if (_cache.TryGet<List<Turno>>(CacheKeyMisTurnos, out var cached) && cached != null)
            return new ApiResponse<List<Turno>> { Success = true, Data = cached };
        var result = await GetAsync<List<Turno>>("/api/turnos/my");
        if (result.Success && result.Data != null)
            _cache.Set(CacheKeyMisTurnos, result.Data, TimeSpan.FromMinutes(2));
        return result;
    }

    public Task<ApiResponse<Turno>> GetTurnoAsync(int id)
        => GetAsync<Turno>($"/api/turnos/{id}");

    public async Task<ApiResponse<Turno>> CrearTurnoAsync(CrearTurnoRequest request)
    {
        var result = await PostAsync<Turno>("/api/turnos", request);
        if (result.Success) { _cache.Invalidate(CacheKeyTurnos); _cache.Invalidate(CacheKeyMisTurnos); }
        return result;
    }

    public async Task<ApiResponse<Turno>> CrearTurnoMobileAsync(ViewModels.CrearTurnoMobileRequest request)
    {
        var apiRequest = new
        {
            DocumentoIdentidad = request.DocumentoIdentidad,
            TipoSolicitud = request.TipoSolicitud,
            Notas = request.Notas,
            Items = request.Items.Select(i => new { Id = i.Id, Cantidad = i.Cantidad }).ToList()
        };
        var result = await PostAsync<Turno>("/api/turnos", apiRequest);
        if (result.Success) { _cache.Invalidate(CacheKeyTurnos); _cache.Invalidate(CacheKeyMisTurnos); }
        return result;
    }

    public Task<ApiResponse<List<int>>> GetRestrictedMedicinesAsync(string documentoIdentidad)
        => GetAsync<List<int>>($"/api/turnos/restricted-medicines/{Uri.EscapeDataString(documentoIdentidad)}");

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

    public async Task<ApiResponse<Turno>> AprobarTurnoAsync(int id, string? comentarios = null)
    {
        var result = await PostAsync<Turno>($"/api/turnos/{id}/approve", new { Comentarios = comentarios });
        if (result.Success) { _cache.Invalidate(CacheKeyTurnos); _cache.Invalidate(CacheKeyMisTurnos); }
        return result;
    }

    public async Task<ApiResponse<Turno>> RechazarTurnoAsync(int id, string motivo)
    {
        var result = await PostAsync<Turno>($"/api/turnos/{id}/reject", new { motivo });
        if (result.Success) { _cache.Invalidate(CacheKeyTurnos); _cache.Invalidate(CacheKeyMisTurnos); }
        return result;
    }

    public async Task<ApiResponse<Turno>> ReprogramarTurnoAsync(int id, DateTime nuevaFecha, string? motivo)
    {
        var result = await PostAsync<Turno>($"/api/turnos/{id}/reschedule", new { nuevaFecha, motivo });
        if (result.Success) { _cache.Invalidate(CacheKeyTurnos); _cache.Invalidate(CacheKeyMisTurnos); }
        return result;
    }

    public async Task<ApiResponse<Turno>> CancelarTurnoAsync(int id, string motivo)
    {
        var result = await PostAsync<Turno>($"/api/turnos/{id}/cancel", new { motivo });
        if (result.Success) { _cache.Invalidate(CacheKeyTurnos); _cache.Invalidate(CacheKeyMisTurnos); }
        return result;
    }

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

    public async Task<ApiResponse<List<Medicine>>> GetMedicamentosAsync()
    {
        if (_cache.TryGet<List<Medicine>>(CacheKeyMedicamentos, out var cached) && cached != null)
            return new ApiResponse<List<Medicine>> { Success = true, Data = cached };

        var result = await GetPagedAsync<Medicine>("/api/medicines?pageSize=2000");
        if (result.Success && result.Data != null)
            _cache.Set(CacheKeyMedicamentos, result.Data);
        return result;
    }

    public Task<ApiResponse<Medicine>> GetMedicamentoAsync(int id)
        => GetAsync<Medicine>($"/api/medicines/{id}");

    public async Task<ApiResponse<Medicine>> CrearMedicamentoAsync(Medicine medicamento)
    {
        var result = await PostAsync<Medicine>("/api/medicines", medicamento);
        if (result.Success) _cache.Invalidate(CacheKeyMedicamentos);
        return result;
    }

    public async Task<ApiResponse<Medicine>> ActualizarMedicamentoAsync(Medicine medicamento)
    {
        var result = await PutAsync<Medicine>($"/api/medicines/{medicamento.Id}", medicamento);
        if (result.Success) _cache.Invalidate(CacheKeyMedicamentos);
        return result;
    }

    public async Task<ApiResponse<bool>> EliminarMedicamentoAsync(int id)
    {
        var result = await DeleteAsync($"/api/medicines/{id}");
        if (result.Success) _cache.Invalidate(CacheKeyMedicamentos);
        return result;
    }

    // === INSUMOS ===

    public async Task<ApiResponse<List<Supply>>> GetInsumosAsync()
    {
        if (_cache.TryGet<List<Supply>>(CacheKeyInsumos, out var cached) && cached != null)
            return new ApiResponse<List<Supply>> { Success = true, Data = cached };

        var result = await GetPagedAsync<Supply>("/api/supplies?pageSize=2000");
        if (result.Success && result.Data != null)
            _cache.Set(CacheKeyInsumos, result.Data);
        return result;
    }

    public Task<ApiResponse<Supply>> GetInsumoAsync(int id)
        => GetAsync<Supply>($"/api/supplies/{id}");

    public async Task<ApiResponse<Supply>> CrearInsumoAsync(Supply insumo)
    {
        var result = await PostAsync<Supply>("/api/supplies", insumo);
        if (result.Success) _cache.Invalidate(CacheKeyInsumos);
        return result;
    }

    public async Task<ApiResponse<Supply>> ActualizarInsumoAsync(Supply insumo)
    {
        var result = await PutAsync<Supply>($"/api/supplies/{insumo.Id}", insumo);
        if (result.Success) _cache.Invalidate(CacheKeyInsumos);
        return result;
    }

    public async Task<ApiResponse<bool>> EliminarInsumoAsync(int id)
    {
        var result = await DeleteAsync($"/api/supplies/{id}");
        if (result.Success) _cache.Invalidate(CacheKeyInsumos);
        return result;
    }

    // === DONACIONES ===

    public async Task<ApiResponse<List<Donation>>> GetDonacionesAsync()
    {
        if (_cache.TryGet<List<Donation>>(CacheKeyDonaciones, out var cached) && cached != null)
            return new ApiResponse<List<Donation>> { Success = true, Data = cached };
        var result = await GetPagedAsync<Donation>("/api/donations?pageSize=500");
        if (result.Success && result.Data != null)
            _cache.Set(CacheKeyDonaciones, result.Data);
        return result;
    }

    public Task<ApiResponse<Donation>> GetDonacionAsync(int id)
        => GetAsync<Donation>($"/api/donations/{id}");

    public async Task<ApiResponse<Donation>> CrearDonacionAsync(Donation donacion)
    {
        var result = await PostAsync<Donation>("/api/donations", donacion);
        if (result.Success) _cache.Invalidate(CacheKeyDonaciones);
        return result;
    }

    public async Task<ApiResponse<Donation>> ActualizarDonacionAsync(Donation donacion)
    {
        var result = await PutAsync<Donation>($"/api/donations/{donacion.Id}", donacion);
        if (result.Success) _cache.Invalidate(CacheKeyDonaciones);
        return result;
    }

    public async Task<ApiResponse<bool>> EliminarDonacionAsync(int id)
    {
        var result = await DeleteAsync($"/api/donations/{id}");
        if (result.Success) _cache.Invalidate(CacheKeyDonaciones);
        return result;
    }

    // === ENTREGAS ===

    public async Task<ApiResponse<List<Delivery>>> GetEntregasAsync()
    {
        if (_cache.TryGet<List<Delivery>>(CacheKeyEntregas, out var cached) && cached != null)
            return new ApiResponse<List<Delivery>> { Success = true, Data = cached };
        var result = await GetPagedAsync<Delivery>("/api/deliveries?pageSize=500");
        if (result.Success && result.Data != null)
            _cache.Set(CacheKeyEntregas, result.Data, TimeSpan.FromMinutes(3));
        return result;
    }

    public Task<ApiResponse<Delivery>> GetEntregaAsync(int id)
        => GetAsync<Delivery>($"/api/deliveries/{id}");

    public async Task<ApiResponse<Delivery>> CrearEntregaAsync(Delivery entrega)
    {
        var result = await PostAsync<Delivery>("/api/deliveries", entrega);
        if (result.Success) _cache.Invalidate(CacheKeyEntregas);
        return result;
    }

    public async Task<ApiResponse<bool>> EliminarEntregaAsync(int id)
    {
        var result = await DeleteAsync($"/api/deliveries/{id}");
        if (result.Success) _cache.Invalidate(CacheKeyEntregas);
        return result;
    }

    public Task<ApiResponse<List<TurnoForDelivery>>> GetTurnosAprobadosByIdentificationAsync(string identification)
        => GetAsync<List<TurnoForDelivery>>($"/api/turnos/by-identification/{Uri.EscapeDataString(identification)}");

    public async Task<ApiResponse<PatientInfo>> GetPatientByIdentificationAsync(string identification)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"/api/patients/by-identification/{Uri.EscapeDataString(identification)}");
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<PatientInfo>>(content, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result ?? new ApiResponse<PatientInfo> { Success = false, Message = "Error deserializando" };
            }
            
            return new ApiResponse<PatientInfo> { Success = false, Message = $"Error: {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PatientInfo> { Success = false, Message = ex.Message };
        }
    }

    public async Task<ApiResponse<Delivery>> CreateDeliveryAsync(CreateDeliveryRequest request)
    {
        var result = await PostAsync<Delivery>("/api/deliveries", request);
        if (result.Success) _cache.Invalidate(CacheKeyEntregas);
        return result;
    }

    // === PACIENTES ===

    public async Task<ApiResponse<List<Patient>>> GetPacientesAsync()
    {
        if (_cache.TryGet<List<Patient>>(CacheKeyPacientes, out var cached) && cached != null)
            return new ApiResponse<List<Patient>> { Success = true, Data = cached };
        var result = await GetPagedAsync<Patient>("/api/patients?pageSize=500");
        if (result.Success && result.Data != null)
            _cache.Set(CacheKeyPacientes, result.Data);
        return result;
    }

    public Task<ApiResponse<Patient>> GetPacienteAsync(int id)
        => GetAsync<Patient>($"/api/patients/{id}");

    public async Task<ApiResponse<Patient>> CrearPacienteAsync(Patient paciente)
    {
        var result = await PostAsync<Patient>("/api/patients", paciente);
        if (result.Success) _cache.Invalidate(CacheKeyPacientes);
        return result;
    }

    public async Task<ApiResponse<Patient>> ActualizarPacienteAsync(Patient paciente)
    {
        var result = await PutAsync<Patient>($"/api/patients/{paciente.Id}", paciente);
        if (result.Success) _cache.Invalidate(CacheKeyPacientes);
        return result;
    }

    public async Task<ApiResponse<bool>> DeletePacienteAsync(int id)
    {
        var result = await DeleteAsync($"/api/patients/{id}");
        if (result.Success) _cache.Invalidate(CacheKeyPacientes);
        return result;
    }

    public Task<ApiResponse<List<PatientDocument>>> GetDocumentosPacienteAsync(int patientId)
        => GetAsync<List<PatientDocument>>($"/api/patients/{patientId}/documents");

    public Task<ApiResponse<TurnoDocumentsSearchResult>> GetTurnoDocumentsByIdentificationAsync(string identification)
        => GetAsync<TurnoDocumentsSearchResult>($"/api/patients/turno-documents/{Uri.EscapeDataString(identification)}");

    public async Task<ApiResponse<ImportDocumentsResult>> ImportTurnoDocumentsAsync(int patientId, List<TurnoDocumentImportItem> documents)
    {
        var request = new { Documents = documents };
        return await PostAsync<ImportDocumentsResult>($"/api/patients/{patientId}/import-turno-documents", request);
    }

    public async Task<byte[]?> DownloadPatientDocumentAsync(int patientId, int documentId)
    {
        try
        {
            await SetAuthHeaderAsync();
            var url = $"/api/patients/{patientId}/documents/{documentId}/download";
            System.Diagnostics.Debug.WriteLine($"[ApiService] Descargando documento: {url}");
            
            var response = await _httpClient.GetAsync(url);
            
            System.Diagnostics.Debug.WriteLine($"[ApiService] Respuesta descarga: {response.StatusCode}");
            
            if (response.IsSuccessStatusCode)
            {
                var bytes = await response.Content.ReadAsByteArrayAsync();
                System.Diagnostics.Debug.WriteLine($"[ApiService] Documento descargado: {bytes.Length} bytes");
                return bytes;
            }
            
            var errorContent = await response.Content.ReadAsStringAsync();
            System.Diagnostics.Debug.WriteLine($"[ApiService] Error descarga documento: {response.StatusCode} - {errorContent}");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiService] Error descargando documento: {ex.Message}");
            return null;
        }
    }

    public async Task<ApiResponse<List<PatientAutoCompleteItem>>> SearchPacientesAutocompleteAsync(string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return new ApiResponse<List<PatientAutoCompleteItem>> { Success = true, Data = new List<PatientAutoCompleteItem>() };
        return await GetAsync<List<PatientAutoCompleteItem>>($"/api/patients/search-autocomplete?q={Uri.EscapeDataString(q)}");
    }

    public async Task<ApiResponse<Patient>> BloquearPacientePrestamoAsync(int id, string description)
    {
        var result = await PostAsync<Patient>($"/api/patients/{id}/block-loan", new BlockPatientLoanRequest { Description = description });
        if (result.Success) _cache.Invalidate(CacheKeyPacientes);
        return result;
    }

    public async Task<ApiResponse<Patient>> DesbloquearPacientePrestamoAsync(int id)
    {
        var result = await PostAsync<Patient>($"/api/patients/{id}/unblock-loan", new { });
        if (result.Success) _cache.Invalidate(CacheKeyPacientes);
        return result;
    }

    public async Task<ApiResponse<PatientDocument>> SubirDocumentoPacienteAsync(
        int patientId, string fileName, string documentType, byte[] fileBytes, string? notes)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"[ApiService] SubirDocumento: patientId={patientId}, fileName={fileName}, bytes={fileBytes?.Length ?? 0}");
            
            if (fileBytes == null || fileBytes.Length == 0)
            {
                return new ApiResponse<PatientDocument> 
                { 
                    Success = false, 
                    Message = "El archivo está vacío" 
                };
            }

            await SetAuthHeaderAsync();

            using var content = new MultipartFormDataContent();
            
            var fileContent = new ByteArrayContent(fileBytes);
            
            // Determinar content type basado en extensión
            string contentType = "application/octet-stream";
            if (fileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                contentType = "application/pdf";
            else if (fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
                     fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                contentType = "image/jpeg";
            else if (fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                contentType = "image/png";
            else if (fileName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
                contentType = "image/webp";
            else if (fileName.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
                contentType = "image/gif";
            
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            fileContent.Headers.ContentLength = fileBytes.Length;
            
            System.Diagnostics.Debug.WriteLine($"[ApiService] ContentType={contentType}, ContentLength={fileBytes.Length}");
            
            content.Add(fileContent, "document", fileName);
            content.Add(new StringContent(documentType), "documentType");
            if (!string.IsNullOrEmpty(notes))
            {
                content.Add(new StringContent(notes), "notes");
            }

            var response = await _httpClient.PostAsync($"/api/patients/{patientId}/documents", content);
            var responseContent = await response.Content.ReadAsStringAsync();
            
            System.Diagnostics.Debug.WriteLine($"[ApiService] Response: {response.StatusCode} - {responseContent}");

            if (response.IsSuccessStatusCode)
            {
                var result = JsonSerializer.Deserialize<ApiResponse<PatientDocument>>(responseContent, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                return result ?? new ApiResponse<PatientDocument> { Success = false, Message = "Error deserializando respuesta" };
            }
            
            return new ApiResponse<PatientDocument> 
            { 
                Success = false, 
                Message = $"Error: {response.StatusCode} - {responseContent}" 
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiService] SubirDocumento Exception: {ex.Message}");
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
                // Ajustar EndDate para incluir todo el día (hasta las 23:59:59.999)
                var adjustedEndDate = fechaFin.HasValue 
                    ? fechaFin.Value.Date.AddDays(1).AddMilliseconds(-1)
                    : fechaFin;
                
                requestBody = new
                {
                    StartDate = fechaInicio,
                    EndDate = adjustedEndDate
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

    public async Task<ApiResponse<bool>> SendVerificationCodeAsync(string email)
    {
        try
        {
            var requestObj = new { Email = email };
            var json = JsonSerializer.Serialize(requestObj, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/auth/send-verification-code", content);
            return await ProcessResponseAsync<bool>(response);
        }
        catch (Exception ex)
        {
            return ErrorResponse<bool>($"Error de conexión: {ex.Message}");
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

    public async Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request)
    {
        try
        {
            await SetAuthHeaderAsync();
            var json = JsonSerializer.Serialize(request, _jsonOptions);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync("/api/auth/change-password", content);
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

    // === USUARIOS (Admin) ===

    public async Task<ApiResponse<List<UserDto>>> GetUsuariosAsync()
    {
        if (_cache.TryGet<List<UserDto>>(CacheKeyUsuarios, out var cached) && cached != null)
            return new ApiResponse<List<UserDto>> { Success = true, Data = cached };
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync("/api/users");
            var result = await ProcessResponseAsync<List<UserDto>>(response);
            if (result.Success && result.Data != null)
                _cache.Set(CacheKeyUsuarios, result.Data);
            return result;
        }
        catch (Exception ex)
        {
            return ErrorResponse<List<UserDto>>($"Error de conexión: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>> GetUsuarioAsync(string id)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"/api/users/{id}");
            return await ProcessResponseAsync<UserDto>(response);
        }
        catch (Exception ex)
        {
            return ErrorResponse<UserDto>($"Error de conexión: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>> CrearUsuarioAsync(CreateUserRequest request)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PostAsJsonAsync("/api/users", request);
            var result = await ProcessResponseAsync<UserDto>(response);
            if (result.Success) _cache.Invalidate(CacheKeyUsuarios);
            return result;
        }
        catch (Exception ex)
        {
            return ErrorResponse<UserDto>($"Error de conexión: {ex.Message}");
        }
    }

    public async Task<ApiResponse<UserDto>> ActualizarUsuarioAsync(string id, UpdateUserRequest request)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.PutAsJsonAsync($"/api/users/{id}", request);
            var result = await ProcessResponseAsync<UserDto>(response);
            if (result.Success) _cache.Invalidate(CacheKeyUsuarios);
            return result;
        }
        catch (Exception ex)
        {
            return ErrorResponse<UserDto>($"Error de conexión: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> EliminarUsuarioAsync(string id)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.DeleteAsync($"/api/users/{id}");
            var result = await ProcessResponseAsync<bool>(response);
            if (result.Success) _cache.Invalidate(CacheKeyUsuarios);
            return result;
        }
        catch (Exception ex)
        {
            return ErrorResponse<bool>($"Error de conexión: {ex.Message}");
        }
    }

    public async Task<ApiResponse<List<string>>> GetRolesAsync()
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync("/api/users/roles");
            return await ProcessResponseAsync<List<string>>(response);
        }
        catch (Exception ex)
        {
            return ErrorResponse<List<string>>($"Error de conexión: {ex.Message}");
        }
    }

    // === REPROGRAMACIÓN DE TURNOS (Admin) ===

    public async Task<ApiResponse<ReprogramarPreviewDto>> GetReprogramarPreviewAsync(DateTime fecha)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"/api/turnos/reprogramar/preview?fecha={fecha:yyyy-MM-dd}");
            return await ProcessResponseAsync<ReprogramarPreviewDto>(response);
        }
        catch (Exception ex)
        {
            return ErrorResponse<ReprogramarPreviewDto>($"Error de conexión: {ex.Message}");
        }
    }

    public async Task<ApiResponse<ReprogramarResultDto>> ReprogramarTurnosAsync(DateTime fechaAfectada, string motivo)
    {
        try
        {
            await SetAuthHeaderAsync();
            // DateTimeKind.Unspecified evita que System.Text.Json incluya offset de zona horaria
            // en el JSON (ej. "-05:00"), lo que causaría que el servidor recibiera la fecha
            // convertida a su zona horaria local y buscara el día incorrecto.
            var fechaNormalizada = DateTime.SpecifyKind(fechaAfectada.Date, DateTimeKind.Unspecified);
            var request = new { FechaAfectada = fechaNormalizada, Motivo = motivo };
            var response = await _httpClient.PostAsJsonAsync("/api/turnos/reprogramar", request);
            var result = await ProcessResponseAsync<ReprogramarResultDto>(response);
            if (result.Success && result.Data?.Reprogramados > 0)
            {
                _cache.Invalidate(CacheKeyTurnos);
                _cache.Invalidate(CacheKeyMisTurnos);
            }
            return result;
        }
        catch (Exception ex)
        {
            return ErrorResponse<ReprogramarResultDto>($"Error de conexión: {ex.Message}");
        }
    }

    // === BROADCAST (Admin) ===

    public Task<ApiResponse<BroadcastResultDto>> SendBroadcastAsync(string title, string message, bool sendEmail, bool sendNotification)
        => PostAsync<BroadcastResultDto>("/api/broadcast/send", new { Title = title, Message = message, SendEmail = sendEmail, SendNotification = sendNotification });

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
