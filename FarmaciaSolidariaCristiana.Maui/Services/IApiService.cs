using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.ViewModels;

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
    Task<ApiResponse<Turno>> CrearTurnoMobileAsync(CrearTurnoMobileRequest request);
    Task<ApiResponse<TurnoDocumentoResponse>> SubirDocumentoTurnoAsync(int turnoId, string fileName, string documentType, byte[] fileBytes, string? description);
    Task<ApiResponse<Turno>> AprobarTurnoAsync(int id, string? comentarios = null);
    Task<ApiResponse<Turno>> RechazarTurnoAsync(int id, string motivo);
    Task<ApiResponse<Turno>> ReprogramarTurnoAsync(int id, DateTime nuevaFecha, string? motivo);
    Task<ApiResponse<Turno>> CancelarTurnoAsync(int id, string motivo);
    Task<ApiResponse<CanCancelTurnoResponse>> PuedeCancelarTurnoAsync(int id);
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
    Task<ApiResponse<List<PatientDocument>>> GetDocumentosPacienteAsync(int patientId);
    Task<ApiResponse<PatientDocument>> SubirDocumentoPacienteAsync(int patientId, string fileName, string documentType, byte[] fileBytes, string? notes);
    Task<ApiResponse<TurnoDocumentsSearchResult>> GetTurnoDocumentsByIdentificationAsync(string identification);
    Task<ApiResponse<ImportDocumentsResult>> ImportTurnoDocumentsAsync(int patientId, List<TurnoDocumentImportItem> documents);
    Task<byte[]?> DownloadPatientDocumentAsync(int patientId, int documentId);
    
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
    
    // Entregas - Nuevos métodos para crear entregas
    Task<ApiResponse<List<TurnoForDelivery>>> GetTurnosAprobadosByIdentificationAsync(string identification);
    Task<ApiResponse<PatientInfo>> GetPatientByIdentificationAsync(string identification);
    Task<ApiResponse<Delivery>> CreateDeliveryAsync(CreateDeliveryRequest request);
    
    // Reportes
    Task<byte[]?> DescargarReporteAsync(string tipoReporte, DateTime? fechaInicio = null, DateTime? fechaFin = null);
    
    // Usuarios (Admin)
    Task<ApiResponse<List<UserDto>>> GetUsuariosAsync();
    Task<ApiResponse<UserDto>> GetUsuarioAsync(string id);
    Task<ApiResponse<UserDto>> CrearUsuarioAsync(CreateUserRequest request);
    Task<ApiResponse<UserDto>> ActualizarUsuarioAsync(string id, UpdateUserRequest request);
    Task<ApiResponse<bool>> EliminarUsuarioAsync(string id);
    Task<ApiResponse<List<string>>> GetRolesAsync();
    
    // Reprogramación de Turnos (Admin)
    Task<ApiResponse<ReprogramarPreviewDto>> GetReprogramarPreviewAsync(DateTime fecha);
    Task<ApiResponse<ReprogramarResultDto>> ReprogramarTurnosAsync(DateTime fechaAfectada, string motivo);
    
    // Autenticación
    Task<ApiResponse<RegistrationStatusDto>> GetRegistrationStatusAsync();
    Task<ApiResponse<bool>> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<bool>> ForgotPasswordAsync(string emailOrUserName);
    Task<ApiResponse<bool>> ChangePasswordAsync(ChangePasswordRequest request);
    
    // Diagnóstico
    Task<bool> CheckApiHealthAsync();
}

/// <summary>
/// Estado del registro público
/// </summary>
public class RegistrationStatusDto
{
    public bool IsEnabled { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Request para registro de usuario
/// </summary>
public class RegisterRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class FechaBloqueadaDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string? Motivo { get; set; }
    public DateTime CreatedAt { get; set; }
}

// DTOs para Usuarios
public class UserDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
}

public class CreateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class UpdateUserRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? NewPassword { get; set; }
    public string NewRole { get; set; } = string.Empty;
}

// DTOs para Reprogramación de Turnos
public class ReprogramarPreviewDto
{
    public DateTime Fecha { get; set; }
    public int TotalTurnos { get; set; }
    public List<TurnoAfectadoDto> Turnos { get; set; } = new();
}

public class TurnoAfectadoDto
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime? FechaPreferida { get; set; }
}

public class ReprogramarResultDto
{
    public int TotalAfectados { get; set; }
    public int Reprogramados { get; set; }
    public int NoReprogramados { get; set; }
    public string Mensaje { get; set; } = string.Empty;
    public List<TurnoReprogramadoDto> TurnosReprogramados { get; set; } = new();
    public List<TurnoNoReprogramadoDto> TurnosNoReprogramados { get; set; } = new();
}

public class TurnoReprogramadoDto
{
    public int TurnoId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? UserEmail { get; set; }
    public DateTime FechaOriginal { get; set; }
    public DateTime FechaNueva { get; set; }
}

public class TurnoNoReprogramadoDto
{
    public int TurnoId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Razon { get; set; } = string.Empty;
}
