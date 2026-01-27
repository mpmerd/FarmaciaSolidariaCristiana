using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class PacientesViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Patient> pacientes = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool canEdit;

    [ObservableProperty]
    private bool isRefreshing;

    private List<Patient> _allPacientes = new();

    public PacientesViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Pacientes";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await AuthService.GetUserInfoAsync();
        CanEdit = userInfo?.Role == Constants.RoleAdmin || 
                  userInfo?.Role == Constants.RoleFarmaceutico;
        await LoadPacientesAsync();
    }

    [RelayCommand]
    private async Task LoadPacientesAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await ApiService.GetPacientesAsync();
            if (response.Success && response.Data != null)
            {
                _allPacientes = response.Data;
                ApplyFilter();
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar pacientes");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadPacientesAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Pacientes = new ObservableCollection<Patient>(_allPacientes);
        }
        else
        {
            var filtered = _allPacientes
                .Where(p => p.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           (p.IdentificationDocument?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false) ||
                           (p.Phone?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            Pacientes = new ObservableCollection<Patient>(filtered);
        }
    }

    [RelayCommand]
    private async Task SelectPacienteAsync(Patient paciente)
    {
        if (paciente == null) return;
        
        var options = CanEdit 
            ? new[] { "Ver detalles", "Editar", "Ver documentos médicos", "Eliminar" } 
            : new[] { "Ver detalles", "Ver documentos médicos" };
        
        var action = await Shell.Current.DisplayActionSheet(
            paciente.FullName,
            "Cancelar",
            null,
            options);

        switch (action)
        {
            case "Ver detalles":
                await ViewDetailsAsync(paciente);
                break;
            case "Editar":
                await EditPacienteAsync(paciente);
                break;
            case "Ver documentos médicos":
                await VerDocumentosMedicosAsync(paciente);
                break;
            case "Eliminar":
                await DeletePacienteAsync(paciente);
                break;
        }
    }

    [RelayCommand]
    private async Task AddPacienteAsync()
    {
        if (!CanEdit)
        {
            await ShowErrorAsync("No tiene permisos para agregar pacientes.");
            return;
        }

        // Nombre completo
        var nombre = await Shell.Current.DisplayPromptAsync(
            "Nuevo Paciente",
            "Nombre completo:",
            maxLength: 200);
        if (string.IsNullOrWhiteSpace(nombre)) return;

        // Cédula
        var cedula = await Shell.Current.DisplayPromptAsync(
            "Nuevo Paciente",
            "Carnet de Identidad (11 dígitos):",
            maxLength: 20);
        if (string.IsNullOrWhiteSpace(cedula)) return;

        // Edad
        var edadStr = await Shell.Current.DisplayPromptAsync(
            "Nuevo Paciente",
            "Edad:",
            keyboard: Keyboard.Numeric);
        if (string.IsNullOrEmpty(edadStr) || !int.TryParse(edadStr, out var edad)) return;

        // Sexo
        var sexo = await Shell.Current.DisplayActionSheet(
            "Sexo del paciente",
            "Cancelar",
            null,
            "Masculino", "Femenino");
        if (sexo == "Cancelar" || sexo == null) return;

        // Teléfono (opcional)
        var telefono = await Shell.Current.DisplayPromptAsync(
            "Nuevo Paciente",
            "Teléfono (opcional):",
            maxLength: 20);
        if (telefono == null) return;

        try
        {
            IsBusy = true;
            var nuevoPaciente = new Patient
            {
                FullName = nombre,
                IdentificationDocument = cedula,
                Age = edad,
                Gender = sexo == "Masculino" ? "M" : "F",
                Phone = string.IsNullOrWhiteSpace(telefono) ? null : telefono,
                IsActive = true
            };
            
            var result = await ApiService.CrearPacienteAsync(nuevoPaciente);

            if (result.Success)
            {
                await Shell.Current.DisplayAlert("Éxito", "Paciente creado correctamente", "OK");
                await LoadPacientesAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al crear paciente");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task ViewDetailsAsync(Patient paciente)
    {
        if (paciente == null) return;
        
        var details = $"Nombre: {paciente.FullName}\n" +
                     $"Cédula: {paciente.IdentificationDocument}\n" +
                     $"Edad: {paciente.Age} años\n" +
                     $"Sexo: {(paciente.Gender == "M" ? "Masculino" : "Femenino")}\n" +
                     $"Teléfono: {paciente.Phone ?? "N/A"}\n" +
                     $"Dirección: {paciente.Address ?? "N/A"}\n" +
                     $"Municipio: {paciente.Municipality ?? "N/A"}\n" +
                     $"Provincia: {paciente.Province ?? "N/A"}\n" +
                     $"Diagnóstico: {paciente.MainDiagnosis ?? "N/A"}\n" +
                     $"Alergias: {paciente.KnownAllergies ?? "N/A"}\n" +
                     $"Entregas realizadas: {paciente.DeliveriesCount}\n" +
                     $"Activo: {(paciente.IsActive ? "Sí" : "No")}";
                     
        await Shell.Current.DisplayAlert("Detalle de Paciente", details, "OK");
    }

    [RelayCommand]
    private async Task EditPacienteAsync(Patient paciente)
    {
        if (!CanEdit || paciente == null)
        {
            await ShowErrorAsync("No tiene permisos para editar pacientes.");
            return;
        }

        // Editar nombre
        var nuevoNombre = await Shell.Current.DisplayPromptAsync(
            "Editar Paciente",
            "Nombre completo:",
            initialValue: paciente.FullName,
            maxLength: 200);
        if (string.IsNullOrEmpty(nuevoNombre)) return;

        // Editar teléfono
        var nuevoTelefono = await Shell.Current.DisplayPromptAsync(
            "Editar Paciente",
            "Teléfono:",
            initialValue: paciente.Phone ?? "",
            maxLength: 20);
        if (nuevoTelefono == null) return;

        // Editar dirección
        var nuevaDireccion = await Shell.Current.DisplayPromptAsync(
            "Editar Paciente",
            "Dirección:",
            initialValue: paciente.Address ?? "",
            maxLength: 500);
        if (nuevaDireccion == null) return;

        try
        {
            IsBusy = true;
            paciente.FullName = nuevoNombre;
            paciente.Phone = string.IsNullOrWhiteSpace(nuevoTelefono) ? null : nuevoTelefono;
            paciente.Address = string.IsNullOrWhiteSpace(nuevaDireccion) ? null : nuevaDireccion;
            
            var result = await ApiService.ActualizarPacienteAsync(paciente);

            if (result.Success)
            {
                await Shell.Current.DisplayAlert("Éxito", "Paciente actualizado correctamente", "OK");
                await LoadPacientesAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al actualizar paciente");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task VerDocumentosMedicosAsync(Patient paciente)
    {
        try
        {
            IsBusy = true;
            var response = await ApiService.GetDocumentosPacienteAsync(paciente.Id);
            
            if (response.Success && response.Data != null && response.Data.Count > 0)
            {
                var docs = response.Data;
                var opciones = docs.Select(d => $"{d.DocumentType}: {d.FileName}").ToArray();
                
                var seleccion = await Shell.Current.DisplayActionSheet(
                    $"Documentos de {paciente.FullName}",
                    "Cerrar",
                    null,
                    opciones);
                
                if (seleccion != "Cerrar" && seleccion != null)
                {
                    var index = Array.IndexOf(opciones, seleccion);
                    if (index >= 0)
                    {
                        var doc = docs[index];
                        await Shell.Current.DisplayAlert(
                            doc.DocumentType,
                            $"Archivo: {doc.FileName}\nSubido: {doc.UploadedAt:dd/MM/yyyy}\nNotas: {doc.Notes ?? "Sin notas"}\n\n(Descarga no disponible en esta versión)",
                            "OK");
                    }
                }
            }
            else
            {
                await Shell.Current.DisplayAlert(
                    "Documentos Médicos",
                    $"No hay documentos médicos registrados para {paciente.FullName}.",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error al cargar documentos: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeletePacienteAsync(Patient paciente)
    {
        if (!CanEdit || paciente == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Eliminar Paciente",
            $"¿Estás seguro de eliminar a '{paciente.FullName}'?",
            "Sí, eliminar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                IsBusy = true;
                var response = await ApiService.DeletePacienteAsync(paciente.Id);
                
                if (response.Success)
                {
                    _allPacientes.Remove(paciente);
                    ApplyFilter();
                    await Shell.Current.DisplayAlert("Éxito", "Paciente eliminado correctamente", "OK");
                }
                else
                {
                    await ShowErrorAsync(response.Message ?? "Error al eliminar");
                }
            }
            catch (Exception ex)
            {
                await ShowErrorAsync($"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
