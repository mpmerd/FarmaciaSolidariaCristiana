using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

/// <summary>
/// ViewModel para registrar una nueva donación
/// </summary>
public partial class NuevaDonacionViewModel : BaseViewModel
{
    #region Observable Properties

    [ObservableProperty]
    private bool _isMedicamento = true;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<ItemBusquedaDonacion> _resultadosBusqueda = new();

    [ObservableProperty]
    private bool _showSearchResults;

    [ObservableProperty]
    private ItemBusquedaDonacion? _itemSeleccionado;

    [ObservableProperty]
    private int _cantidad = 1;

    [ObservableProperty]
    private string _cantidadError = string.Empty;

    [ObservableProperty]
    private DateTime _fechaDonacion = DateTime.Today;

    [ObservableProperty]
    private DateTime _maxDate = DateTime.Today;

    [ObservableProperty]
    private string _notaDonante = string.Empty;

    [ObservableProperty]
    private string _comentarios = string.Empty;

    [ObservableProperty]
    private bool _canSubmit;

    #endregion

    private List<Medicine> _allMedicamentos = new();
    private List<Supply> _allInsumos = new();

    public NuevaDonacionViewModel(IAuthService authService, IApiService apiService)
        : base(authService, apiService)
    {
        Title = "Nueva Donación";
    }

    #region Property Changed Handlers

    partial void OnIsMedicamentoChanged(bool value)
    {
        // Limpiar selección al cambiar tipo
        ClearSelection();
        ResultadosBusqueda.Clear();
        SearchText = string.Empty;
        ShowSearchResults = false;
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterItems();
    }

    partial void OnCantidadChanged(int value)
    {
        ValidateCantidad();
        UpdateCanSubmit();
    }

    partial void OnItemSeleccionadoChanged(ItemBusquedaDonacion? value)
    {
        UpdateCanSubmit();
    }

    #endregion

    #region Validation

    private void ValidateCantidad()
    {
        if (Cantidad <= 0)
        {
            CantidadError = "La cantidad debe ser mayor que 0";
        }
        else
        {
            CantidadError = string.Empty;
        }
    }

    private void UpdateCanSubmit()
    {
        CanSubmit = ItemSeleccionado != null && Cantidad > 0;
    }

    #endregion

    #region Commands

    [RelayCommand]
    public async Task InitializeAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        try
        {
            // Cargar medicamentos e insumos disponibles
            var medTask = ApiService.GetMedicamentosAsync();
            var insTask = ApiService.GetInsumosAsync();

            await Task.WhenAll(medTask, insTask);

            var medResult = await medTask;
            var insResult = await insTask;

            if (medResult.Success && medResult.Data != null)
            {
                _allMedicamentos = medResult.Data;
            }

            if (insResult.Success && insResult.Data != null)
            {
                _allInsumos = insResult.Data;
            }
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Error al cargar datos: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SetMedicamento()
    {
        IsMedicamento = true;
    }

    [RelayCommand]
    private void SetInsumo()
    {
        IsMedicamento = false;
    }

    private void FilterItems()
    {
        if (string.IsNullOrWhiteSpace(SearchText) || SearchText.Length < 2)
        {
            ResultadosBusqueda.Clear();
            ShowSearchResults = false;
            return;
        }

        var search = SearchText.ToLower();
        var results = new List<ItemBusquedaDonacion>();

        if (IsMedicamento)
        {
            results = _allMedicamentos
                .Where(m => m.Name.ToLower().Contains(search) ||
                           (m.Description?.ToLower().Contains(search) ?? false))
                .Take(10)
                .Select(m => new ItemBusquedaDonacion
                {
                    Id = m.Id,
                    Name = m.Name,
                    Stock = m.StockQuantity,
                    Unit = m.Unit,
                    IsMedicamento = true
                })
                .ToList();
        }
        else
        {
            results = _allInsumos
                .Where(s => s.Name.ToLower().Contains(search) ||
                           (s.Description?.ToLower().Contains(search) ?? false))
                .Take(10)
                .Select(s => new ItemBusquedaDonacion
                {
                    Id = s.Id,
                    Name = s.Name,
                    Stock = s.StockQuantity,
                    Unit = s.Unit,
                    IsMedicamento = false
                })
                .ToList();
        }

        ResultadosBusqueda = new ObservableCollection<ItemBusquedaDonacion>(results);
        ShowSearchResults = results.Count > 0;
    }

    [RelayCommand]
    private void SelectItem(ItemBusquedaDonacion item)
    {
        if (item == null) return;

        ItemSeleccionado = item;
        SearchText = string.Empty;
        ResultadosBusqueda.Clear();
        ShowSearchResults = false;
        Cantidad = 1;
        ValidateCantidad();
        UpdateCanSubmit();
    }

    [RelayCommand]
    private void ClearSelection()
    {
        ItemSeleccionado = null;
        Cantidad = 1;
        NotaDonante = string.Empty;
        Comentarios = string.Empty;
        CantidadError = string.Empty;
        UpdateCanSubmit();
    }

    [RelayCommand]
    private void Increment()
    {
        Cantidad++;
    }

    [RelayCommand]
    private void Decrement()
    {
        if (Cantidad > 1)
        {
            Cantidad--;
        }
    }

    [RelayCommand]
    private async Task SubmitAsync()
    {
        if (!CanSubmit || ItemSeleccionado == null)
        {
            await ShowErrorAsync("Por favor seleccione un item y especifique la cantidad");
            return;
        }

        if (IsBusy) return;

        var confirm = await ShowConfirmAsync(
            "Confirmar Donación",
            $"¿Registrar donación de {Cantidad} unidades de {ItemSeleccionado.Name}?\n\n" +
            "Esto incrementará el stock disponible.");

        if (!confirm) return;

        IsBusy = true;
        try
        {
            var donation = new Donation
            {
                MedicineId = ItemSeleccionado.IsMedicamento ? ItemSeleccionado.Id : null,
                SupplyId = !ItemSeleccionado.IsMedicamento ? ItemSeleccionado.Id : null,
                Quantity = Cantidad,
                DonationDate = FechaDonacion,
                DonorNote = string.IsNullOrWhiteSpace(NotaDonante) ? null : NotaDonante.Trim(),
                Comments = string.IsNullOrWhiteSpace(Comentarios) ? null : Comentarios.Trim()
            };

            var result = await ApiService.CrearDonacionAsync(donation);

            if (result.Success)
            {
                await ShowSuccessAsync($"¡Donación registrada exitosamente!\n\n" +
                    $"Se han añadido {Cantidad} unidades de {ItemSeleccionado.Name} al stock.");
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al registrar la donación");
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

    [RelayCommand]
    private async Task CancelAsync()
    {
        if (ItemSeleccionado != null)
        {
            var confirm = await ShowConfirmAsync(
                "Cancelar Donación",
                "¿Está seguro? Se perderán los datos ingresados.");

            if (!confirm) return;
        }

        await Shell.Current.GoToAsync("..");
    }

    #endregion
}

#region Models

/// <summary>
/// Item de búsqueda para donaciones
/// </summary>
public class ItemBusquedaDonacion
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Stock { get; set; }
    public string Unit { get; set; } = string.Empty;
    public bool IsMedicamento { get; set; }

    public string StockDisplay => $"{Stock} {Unit}";
}

#endregion
