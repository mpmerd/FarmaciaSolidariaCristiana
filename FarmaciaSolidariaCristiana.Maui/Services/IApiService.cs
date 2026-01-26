using FarmaciaSolidariaCristiana.Maui.Models;

namespace FarmaciaSolidariaCristiana.Maui.Services;

/// <summary>
/// Interfaz para el servicio de API
/// </summary>
public interface IApiService
{
    // Turnos
    Task<ApiResponse<List<Turno>>> GetTurnosAsync();
    Task<ApiResponse<List<Turno>>> GetMisTurnosAsync();
    Task<ApiResponse<Turno>> GetTurnoAsync(int id);
    Task<ApiResponse<Turno>> CrearTurnoAsync(CrearTurnoRequest request);
    Task<ApiResponse<Turno>> AprobarTurnoAsync(int id, DateTime fechaAsignada, string? notas);
    Task<ApiResponse<Turno>> RechazarTurnoAsync(int id, string motivo);
    Task<ApiResponse<bool>> CancelarTurnoAsync(int id);
    Task<byte[]?> DescargarTurnoPdfAsync(int id);
    
    // Medicamentos
    Task<ApiResponse<List<Medicine>>> GetMedicamentosAsync();
    Task<ApiResponse<Medicine>> GetMedicamentoAsync(int id);
    Task<ApiResponse<Medicine>> CrearMedicamentoAsync(Medicine medicamento);
    Task<ApiResponse<Medicine>> ActualizarMedicamentoAsync(Medicine medicamento);
    Task<ApiResponse<bool>> EliminarMedicamentoAsync(int id);
    
    // Insumos
    Task<ApiResponse<List<Supply>>> GetInsumosAsync();
    Task<ApiResponse<Supply>> GetInsumoAsync(int id);
    Task<ApiResponse<Supply>> CrearInsumoAsync(Supply insumo);
    Task<ApiResponse<Supply>> ActualizarInsumoAsync(Supply insumo);
    Task<ApiResponse<bool>> EliminarInsumoAsync(int id);
    
    // Donaciones
    Task<ApiResponse<List<Donation>>> GetDonacionesAsync();
    Task<ApiResponse<Donation>> GetDonacionAsync(int id);
    Task<ApiResponse<Donation>> CrearDonacionAsync(Donation donacion);
    Task<ApiResponse<Donation>> ActualizarDonacionAsync(Donation donacion);
    Task<ApiResponse<bool>> EliminarDonacionAsync(int id);
    
    // Entregas
    Task<ApiResponse<List<Delivery>>> GetEntregasAsync();
    Task<ApiResponse<Delivery>> GetEntregaAsync(int id);
    Task<ApiResponse<Delivery>> CrearEntregaAsync(Delivery entrega);
    Task<ApiResponse<bool>> EliminarEntregaAsync(int id);
    
    // Pacientes
    Task<ApiResponse<List<Patient>>> GetPacientesAsync();
    Task<ApiResponse<Patient>> GetPacienteAsync(int id);
    Task<ApiResponse<Patient>> CrearPacienteAsync(Patient paciente);
    Task<ApiResponse<Patient>> ActualizarPacienteAsync(Patient paciente);
    Task<ApiResponse<bool>> DeletePacienteAsync(int id);
    
    // Patrocinadores
    Task<ApiResponse<List<Sponsor>>> GetPatrocinadoresAsync();
    
    // Fechas Bloqueadas (Admin)
    Task<ApiResponse<List<FechaBloqueadaDto>>> GetFechasBloqueadasAsync();
    Task<ApiResponse<FechaBloqueadaDto>> CreateFechaBloqueadaAsync(object request);
    Task<ApiResponse<bool>> DeleteFechaBloqueadaAsync(int id);
    
    // Insumos Delete
    Task<ApiResponse<bool>> DeleteInsumoAsync(int id);
    
    // Donaciones Delete
    Task<ApiResponse<bool>> DeleteDonacionAsync(int id);
    
    // Entregas Delete
    Task<ApiResponse<bool>> DeleteEntregaAsync(int id);
    
    // Reportes
    Task<byte[]?> DescargarReporteAsync(string tipoReporte, DateTime? fechaInicio = null, DateTime? fechaFin = null);
    
    // Diagnóstico
    Task<bool> CheckApiHealthAsync();
}

public class FechaBloqueadaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string? Motivo { get; set; }
    public DateTime CreatedAt { get; set; }
}
