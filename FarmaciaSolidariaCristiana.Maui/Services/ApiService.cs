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
        
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return ErrorResponse<T>(Constants.SesionExpirada);
        }

        var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
        return result ?? ErrorResponse<T>("Error al procesar respuesta");
    }

    private static ApiResponse<T> ErrorResponse<T>(string message)
    {
        return new ApiResponse<T> { Success = false, Message = message };
    }

    // === TURNOS ===
    
    public Task<ApiResponse<List<Turno>>> GetTurnosAsync()
        => GetAsync<List<Turno>>("/api/turnos");

    public Task<ApiResponse<List<Turno>>> GetMisTurnosAsync()
        => GetAsync<List<Turno>>("/api/turnos/mis-turnos");

    public Task<ApiResponse<Turno>> GetTurnoAsync(int id)
        => GetAsync<Turno>($"/api/turnos/{id}");

    public Task<ApiResponse<Turno>> CrearTurnoAsync(CrearTurnoRequest request)
        => PostAsync<Turno>("/api/turnos", request);

    public Task<ApiResponse<Turno>> AprobarTurnoAsync(int id, DateTime fechaAsignada, string? notas)
        => PostAsync<Turno>($"/api/turnos/{id}/aprobar", new { fechaAsignada, notas });

    public Task<ApiResponse<Turno>> RechazarTurnoAsync(int id, string motivo)
        => PostAsync<Turno>($"/api/turnos/{id}/rechazar", new { motivo });

    public Task<ApiResponse<bool>> CancelarTurnoAsync(int id)
        => PostAsync<bool>($"/api/turnos/{id}/cancelar", new { });

    public async Task<byte[]?> DescargarTurnoPdfAsync(int id)
    {
        try
        {
            await SetAuthHeaderAsync();
            var response = await _httpClient.GetAsync($"/api/turnos/{id}/pdf");
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    // === MEDICAMENTOS ===

    public Task<ApiResponse<List<Medicine>>> GetMedicamentosAsync()
        => GetAsync<List<Medicine>>("/api/medicines");

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
        => GetAsync<List<Supply>>("/api/supplies");

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
        => GetAsync<List<Donation>>("/api/donations");

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
        => GetAsync<List<Delivery>>("/api/deliveries");

    public Task<ApiResponse<Delivery>> GetEntregaAsync(int id)
        => GetAsync<Delivery>($"/api/deliveries/{id}");

    public Task<ApiResponse<Delivery>> CrearEntregaAsync(Delivery entrega)
        => PostAsync<Delivery>("/api/deliveries", entrega);

    public Task<ApiResponse<bool>> EliminarEntregaAsync(int id)
        => DeleteAsync($"/api/deliveries/{id}");

    // === PACIENTES ===

    public Task<ApiResponse<List<Patient>>> GetPacientesAsync()
        => GetAsync<List<Patient>>("/api/patients");

    public Task<ApiResponse<Patient>> GetPacienteAsync(int id)
        => GetAsync<Patient>($"/api/patients/{id}");

    public Task<ApiResponse<Patient>> CrearPacienteAsync(Patient paciente)
        => PostAsync<Patient>("/api/patients", paciente);

    public Task<ApiResponse<Patient>> ActualizarPacienteAsync(Patient paciente)
        => PutAsync<Patient>($"/api/patients/{paciente.Id}", paciente);

    public Task<ApiResponse<bool>> DeletePacienteAsync(int id)
        => DeleteAsync($"/api/patients/{id}");

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
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                url += $"?fechaInicio={fechaInicio:yyyy-MM-dd}&fechaFin={fechaFin:yyyy-MM-dd}";
            }
            
            var response = await _httpClient.GetAsync(url);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsByteArrayAsync();
            }
            return null;
        }
        catch
        {
            return null;
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
}
