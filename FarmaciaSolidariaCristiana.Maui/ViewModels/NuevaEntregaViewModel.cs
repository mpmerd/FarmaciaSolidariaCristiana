using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

/// <summary>
/// ViewModel para crear nueva entrega
/// </summary>
public partial class NuevaEntregaViewModel : BaseViewModel
{
    #region Campos

    [ObservableProperty]
    private string documentoIdentidad = string.Empty;

    [ObservableProperty]
    private string documentoError = string.Empty;

    [ObservableProperty]
    private bool isDocumentoValid;

    [ObservableProperty]
    private bool pacienteEncontrado;

    [ObservableProperty]
    private bool pacienteNoEncontrado;

    [ObservableProperty]
    private string pacienteNombre = string.Empty;

    [ObservableProperty]
    private int? pacienteEdad;

    [ObservableProperty]
    private int? pacienteId;

    [ObservableProperty]
    private ObservableCollection<TurnoForDelivery> turnosAprobados = new();

    [ObservableProperty]
    private TurnoForDelivery? turnoSeleccionado;

    [ObservableProperty]
    private bool tieneTurnosAprobados;

    [ObservableProperty]
    private bool mostrarSeleccionItems;

    [ObservableProperty]
    private bool esEntregaSinTurno;

    [ObservableProperty]
    private bool esMedicamento = true;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool showSearchResults;

    [ObservableProperty]
    private ObservableCollection<ItemBusquedaEntrega> resultadosBusqueda = new();

    [ObservableProperty]
    private ObservableCollection<ItemEntrega> itemsAEntregar = new();

    [ObservableProperty]
    private string comentarios = string.Empty;

    [ObservableProperty]
    private bool puedeRegistrar;

    [ObservableProperty]
    private string tituloSeccionItems = "📦 Items a Entregar";

    private List<Medicine> _allMedicines = new();
    private List<Supply> _allSupplies = new();

    #endregion

    public NuevaEntregaViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Nueva Entrega";
    }

    public async Task InitializeAsync()
    {
        // Pre-cargar medicamentos e insumos para búsqueda
        try
        {
            var medsResponse = await ApiService.GetMedicamentosAsync();
            if (medsResponse.Success && medsResponse.Data != null)
            {
                _allMedicines = medsResponse.Data;
            }

            var suppliesResponse = await ApiService.GetInsumosAsync();
            if (suppliesResponse.Success && suppliesResponse.Data != null)
            {
                _allSupplies = suppliesResponse.Data;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cargando catálogos: {ex.Message}");
        }
    }

    #region Validación de Documento

    partial void OnDocumentoIdentidadChanged(string value)
    {
        ValidarDocumento(value);
    }

    private void ValidarDocumento(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            DocumentoError = string.Empty;
            IsDocumentoValid = false;
            return;
        }

        // CI: 11 dígitos, Pasaporte: 1-3 letras + 6-7 dígitos
        var ciPattern = @"^\d{11}$";
        var passportPattern = @"^[A-Za-z]{1,3}\d{6,7}$";

        if (Regex.IsMatch(value, ciPattern) || Regex.IsMatch(value, passportPattern))
        {
            DocumentoError = string.Empty;
            IsDocumentoValid = true;
        }
        else
        {
            DocumentoError = "Formato inválido. Use 11 dígitos para CI o 1-3 letras + 6-7 dígitos para pasaporte.";
            IsDocumentoValid = false;
        }
    }

    #endregion

    #region Comandos

    [RelayCommand]
    private async Task BuscarPacienteAsync()
    {
        if (!IsDocumentoValid)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Por favor ingrese un documento válido", "OK");
            return;
        }

        try
        {
            IsBusy = true;
            
            // Limpiar estado anterior
            PacienteEncontrado = false;
            PacienteNoEncontrado = false;
            TurnosAprobados.Clear();
            TieneTurnosAprobados = false;
            MostrarSeleccionItems = false;
            ItemsAEntregar.Clear();
            TurnoSeleccionado = null;
            EsEntregaSinTurno = false;

            // Buscar paciente
            var patientResponse = await ApiService.GetPatientByIdentificationAsync(DocumentoIdentidad.Trim().ToUpper());
            
            if (patientResponse.Success && patientResponse.Data != null)
            {
                PacienteEncontrado = true;
                PacienteNombre = patientResponse.Data.FullName;
                PacienteEdad = patientResponse.Data.Age;
                PacienteId = patientResponse.Data.Id;

                // Buscar turnos aprobados
                var turnosResponse = await ApiService.GetTurnosAprobadosByIdentificationAsync(DocumentoIdentidad.Trim().ToUpper());
                if (turnosResponse.Success && turnosResponse.Data != null && turnosResponse.Data.Any())
                {
                    TurnosAprobados = new ObservableCollection<TurnoForDelivery>(turnosResponse.Data);
                    TieneTurnosAprobados = true;
                }
                else
                {
                    // Paciente existe pero sin turnos aprobados
                    TieneTurnosAprobados = false;
                    EsEntregaSinTurno = true;
                    MostrarSeleccionItems = true;
                    TituloSeccionItems = "📦 Entrega Sin Turno";
                }
            }
            else
            {
                // Paciente no encontrado - Las entregas SOLO pueden hacerse a pacientes registrados
                await Shell.Current.DisplayAlertAsync(
                    "Paciente No Registrado",
                    "No se encontró un paciente con este documento de identidad.\n\n" +
                    "Para realizar una entrega, primero debe crear la ficha del paciente en la sección 'Pacientes'.\n\n" +
                    "Una vez creado el paciente, podrá regresar aquí para registrar la entrega.",
                    "Entendido");
                
                PacienteNoEncontrado = true;
                PacienteId = null;
                EsEntregaSinTurno = false;
                MostrarSeleccionItems = false;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error al buscar: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    partial void OnTurnoSeleccionadoChanged(TurnoForDelivery? value)
    {
        if (value != null)
        {
            EsEntregaSinTurno = false;
            MostrarSeleccionItems = true;
            TituloSeccionItems = $"📦 Items del Turno #{value.NumeroTurno ?? value.Id}";
            
            // Cargar items del turno
            ItemsAEntregar.Clear();
            
            foreach (var med in value.Medicamentos)
            {
                ItemsAEntregar.Add(new ItemEntrega
                {
                    Id = med.Id,
                    Nombre = med.Nombre,
                    Tipo = "Medicamento",
                    CantidadAprobada = med.CantidadAprobada ?? med.CantidadSolicitada,
                    CantidadAEntregar = med.CantidadAprobada ?? med.CantidadSolicitada,
                    StockActual = med.StockActual,
                    Unidad = med.Unidad
                });
            }
            
            foreach (var ins in value.Insumos)
            {
                ItemsAEntregar.Add(new ItemEntrega
                {
                    Id = ins.Id,
                    Nombre = ins.Nombre,
                    Tipo = "Insumo",
                    CantidadAprobada = ins.CantidadAprobada ?? ins.CantidadSolicitada,
                    CantidadAEntregar = ins.CantidadAprobada ?? ins.CantidadSolicitada,
                    StockActual = ins.StockActual,
                    Unidad = ins.Unidad
                });
            }
            
            ActualizarPuedeRegistrar();
        }
    }

    [RelayCommand]
    private void ContinuarSinTurno()
    {
        TurnoSeleccionado = null;
        EsEntregaSinTurno = true;
        MostrarSeleccionItems = true;
        TituloSeccionItems = "📦 Entrega Sin Turno";
        ItemsAEntregar.Clear();
        ActualizarPuedeRegistrar();
    }

    [RelayCommand]
    private void SeleccionarTurno(TurnoForDelivery turno)
    {
        if (turno != null)
        {
            TurnoSeleccionado = turno;
        }
    }

    [RelayCommand]
    private void SetMedicamento()
    {
        EsMedicamento = true;
        SearchText = string.Empty;
        ShowSearchResults = false;
    }

    [RelayCommand]
    private void SetInsumo()
    {
        EsMedicamento = false;
        SearchText = string.Empty;
        ShowSearchResults = false;
    }

    partial void OnSearchTextChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            ShowSearchResults = false;
            ResultadosBusqueda.Clear();
            return;
        }

        // Buscar en el catálogo correspondiente
        var resultados = new List<ItemBusquedaEntrega>();

        if (EsMedicamento)
        {
            resultados = _allMedicines
                .Where(m => m.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(m => new ItemBusquedaEntrega
                {
                    Id = m.Id,
                    Name = m.Name,
                    Stock = m.StockQuantity,
                    Unit = m.Unit ?? "",
                    Tipo = "Medicamento"
                })
                .ToList();
        }
        else
        {
            resultados = _allSupplies
                .Where(s => s.Name.Contains(value, StringComparison.OrdinalIgnoreCase))
                .Take(10)
                .Select(s => new ItemBusquedaEntrega
                {
                    Id = s.Id,
                    Name = s.Name,
                    Stock = s.StockQuantity,
                    Unit = s.Unit ?? "",
                    Tipo = "Insumo"
                })
                .ToList();
        }

        ResultadosBusqueda = new ObservableCollection<ItemBusquedaEntrega>(resultados);
        ShowSearchResults = resultados.Any();
    }

    [RelayCommand]
    private void AgregarItem(ItemBusquedaEntrega item)
    {
        if (item == null) return;

        // Verificar si ya está agregado
        if (ItemsAEntregar.Any(i => i.Id == item.Id && i.Tipo == item.Tipo))
        {
            Shell.Current.DisplayAlertAsync("Aviso", "Este item ya está en la lista", "OK");
            return;
        }

        ItemsAEntregar.Add(new ItemEntrega
        {
            Id = item.Id,
            Nombre = item.Name,
            Tipo = item.Tipo,
            CantidadAprobada = item.Stock, // En entrega sin turno, el máximo es el stock
            CantidadAEntregar = 1,
            StockActual = item.Stock,
            Unidad = item.Unit
        });

        SearchText = string.Empty;
        ShowSearchResults = false;
        ActualizarPuedeRegistrar();
    }

    [RelayCommand]
    private void RemoveItem(ItemEntrega item)
    {
        if (item != null)
        {
            ItemsAEntregar.Remove(item);
            ActualizarPuedeRegistrar();
        }
    }

    private void ActualizarPuedeRegistrar()
    {
        PuedeRegistrar = IsDocumentoValid && ItemsAEntregar.Any() && ItemsAEntregar.All(i => i.CantidadAEntregar > 0);
    }

    [RelayCommand]
    private async Task RegistrarEntregaAsync()
    {
        if (!PuedeRegistrar)
        {
            await Shell.Current.DisplayAlertAsync("Error", "Complete todos los campos requeridos", "OK");
            return;
        }

        // Validar cantidades
        foreach (var item in ItemsAEntregar)
        {
            if (item.CantidadAEntregar <= 0)
            {
                await Shell.Current.DisplayAlertAsync("Error", $"La cantidad de {item.Nombre} debe ser mayor a 0", "OK");
                return;
            }

            if (!EsEntregaSinTurno && item.CantidadAEntregar > item.CantidadAprobada)
            {
                await Shell.Current.DisplayAlertAsync("Error", 
                    $"No puede entregar más de lo aprobado para {item.Nombre} ({item.CantidadAprobada} unidades)", "OK");
                return;
            }

            if (EsEntregaSinTurno && item.CantidadAEntregar > item.StockActual)
            {
                await Shell.Current.DisplayAlertAsync("Error", 
                    $"Stock insuficiente para {item.Nombre}. Disponible: {item.StockActual}", "OK");
                return;
            }
        }

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Confirmar Entrega",
            $"¿Registrar {ItemsAEntregar.Count} item(s) para el paciente?",
            "Sí, registrar",
            "Cancelar");

        if (!confirm) return;

        try
        {
            IsBusy = true;
            int entregasExitosas = 0;
            var errores = new List<string>();

            foreach (var item in ItemsAEntregar)
            {
                var request = new CreateDeliveryRequest
                {
                    PatientIdentification = DocumentoIdentidad.Trim().ToUpper(),
                    PatientId = PacienteId,
                    TurnoId = TurnoSeleccionado?.Id,
                    Quantity = item.CantidadAEntregar,
                    DeliveryDate = DateTime.Now,
                    Comments = Comentarios
                };

                if (item.Tipo == "Medicamento")
                {
                    request.MedicineId = item.Id;
                }
                else
                {
                    request.SupplyId = item.Id;
                }

                var response = await ApiService.CreateDeliveryAsync(request);
                
                if (response.Success)
                {
                    entregasExitosas++;
                }
                else
                {
                    errores.Add($"{item.Nombre}: {response.Message}");
                }
            }

            if (entregasExitosas > 0)
            {
                string mensaje = $"{entregasExitosas} entrega(s) registrada(s) exitosamente.";
                if (errores.Any())
                {
                    mensaje += $"\n\nErrores:\n{string.Join("\n", errores)}";
                }
                
                await Shell.Current.DisplayAlertAsync("Éxito", mensaje, "OK");
                
                // Volver a la lista de entregas
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", 
                    $"No se pudo registrar ninguna entrega.\n\n{string.Join("\n", errores)}", "OK");
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

    #endregion
}

#region Clases auxiliares

/// <summary>
/// Item de búsqueda para entregas
/// </summary>
public class ItemBusquedaEntrega
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    
    public string StockDisplay => $"{Stock} {Unit}";
    public Color StockColor => Stock > 0 ? Colors.Green : Colors.Red;
}

/// <summary>
/// Item a entregar
/// </summary>
public class ItemEntrega : ObservableObject
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Tipo { get; set; } = string.Empty;
    public int CantidadAprobada { get; set; }
    
    private int _cantidadAEntregar;
    public int CantidadAEntregar 
    { 
        get => _cantidadAEntregar;
        set => SetProperty(ref _cantidadAEntregar, value);
    }
    
    public int StockActual { get; set; }
    public string Unidad { get; set; } = string.Empty;
}

#endregion
