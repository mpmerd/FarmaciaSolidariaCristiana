using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

/// <summary>
/// ViewModel para la gestión de Medicamentos
/// </summary>
public partial class MedicamentosViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Medicine> _medicamentos = new();

    [ObservableProperty]
    private Medicine? _selectedMedicamento;

    [ObservableProperty]
    private bool _canEdit;

    [ObservableProperty]
    private string _searchText = string.Empty;

    private List<Medicine> _allMedicamentos = new();

    public MedicamentosViewModel(IAuthService authService, IApiService apiService) 
        : base(authService, apiService)
    {
        Title = "Medicamentos";
    }

    [RelayCommand]
    public async Task LoadMedicamentosAsync()
    {
        await ExecuteAsync(async () =>
        {
            CanEdit = await AuthService.IsInAnyRoleAsync(
                Constants.RoleAdmin, Constants.RoleFarmaceutico);

            var result = await ApiService.GetMedicamentosAsync();

            if (result.Success && result.Data != null)
            {
                _allMedicamentos = result.Data
                    .OrderBy(m => m.Name)
                    .ToList();
                ApplyFilters();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al cargar medicamentos");
            }
        });
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var filtered = _allMedicamentos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.ToLower();
            filtered = filtered.Where(m =>
                m.Name.ToLower().Contains(search) ||
                (m.Description?.ToLower().Contains(search) ?? false) ||
                (m.NationalCode?.ToLower().Contains(search) ?? false));
        }

        Medicamentos = new ObservableCollection<Medicine>(filtered);
    }

    [RelayCommand]
    private async Task SelectMedicamentoAsync(Medicine medicamento)
    {
        SelectedMedicamento = medicamento;
        
        var options = CanEdit 
            ? new[] { "Ver detalles", "Editar nombre/descripción", "Ajustar stock" } 
            : new[] { "Ver detalles" };
        
        var action = await Shell.Current.DisplayActionSheet(
            medicamento.Name,
            "Cancelar",
            null,
            options);

        switch (action)
        {
            case "Ver detalles":
                await ShowMedicamentoDetailsAsync(medicamento);
                break;
            case "Editar nombre/descripción":
                await EditMedicamentoAsync(medicamento);
                break;
            case "Ajustar stock":
                await AjustarStockAsync(medicamento);
                break;
        }
    }

    private async Task ShowMedicamentoDetailsAsync(Medicine m)
    {
        var details = $"Nombre: {m.Name}\n" +
                      $"Descripción: {m.Description ?? "N/A"}\n" +
                      $"Stock: {m.StockQuantity} {m.Unit}\n" +
                      $"Estado: {m.StockStatus}\n" +
                      $"Código Nacional: {m.NationalCode ?? "N/A"}";

        await Shell.Current.DisplayAlert("Detalles del Medicamento", details, "Cerrar");
    }

    private async Task EditMedicamentoAsync(Medicine medicamento)
    {
        // Editar nombre
        var nuevoNombre = await Shell.Current.DisplayPromptAsync(
            "Editar Medicamento",
            "Nombre del medicamento:",
            initialValue: medicamento.Name,
            maxLength: 200);

        if (string.IsNullOrEmpty(nuevoNombre)) return;

        // Editar descripción
        var nuevaDescripcion = await Shell.Current.DisplayPromptAsync(
            "Editar Medicamento",
            "Descripción:",
            initialValue: medicamento.Description ?? "",
            maxLength: 500);

        // Puede ser vacía la descripción
        if (nuevaDescripcion == null) return;

        await ExecuteAsync(async () =>
        {
            medicamento.Name = nuevoNombre;
            medicamento.Description = string.IsNullOrWhiteSpace(nuevaDescripcion) ? null : nuevaDescripcion;
            
            var result = await ApiService.ActualizarMedicamentoAsync(medicamento);

            if (result.Success)
            {
                await ShowSuccessAsync("Medicamento actualizado");
                await LoadMedicamentosAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al actualizar medicamento");
            }
        });
    }

    private async Task AjustarStockAsync(Medicine medicamento)
    {
        var input = await Shell.Current.DisplayPromptAsync(
            "Ajustar Stock",
            $"Stock actual: {medicamento.StockQuantity}\nIngrese el nuevo stock:",
            placeholder: medicamento.StockQuantity.ToString(),
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrEmpty(input)) return;

        if (!int.TryParse(input, out var newStock) || newStock < 0)
        {
            await ShowErrorAsync("Stock inválido");
            return;
        }

        await ExecuteAsync(async () =>
        {
            medicamento.StockQuantity = newStock;
            var result = await ApiService.ActualizarMedicamentoAsync(medicamento);

            if (result.Success)
            {
                await ShowSuccessAsync("Stock actualizado");
                await LoadMedicamentosAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al actualizar stock");
            }
        });
    }

    [RelayCommand]
    private async Task CreateMedicamentoAsync()
    {
        if (!CanEdit)
        {
            await ShowErrorAsync("No tiene permisos para crear medicamentos.");
            return;
        }

        // Nombre del nuevo medicamento
        var nombre = await Shell.Current.DisplayPromptAsync(
            "Nuevo Medicamento",
            "Nombre del medicamento:",
            maxLength: 200);

        if (string.IsNullOrWhiteSpace(nombre)) return;

        // Descripción
        var descripcion = await Shell.Current.DisplayPromptAsync(
            "Nuevo Medicamento",
            "Descripción (opcional):",
            maxLength: 500);

        if (descripcion == null) return;

        // Stock inicial
        var stockStr = await Shell.Current.DisplayPromptAsync(
            "Nuevo Medicamento",
            "Stock inicial:",
            placeholder: "0",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrEmpty(stockStr)) return;
        if (!int.TryParse(stockStr, out var stock) || stock < 0)
        {
            await ShowErrorAsync("Stock inválido");
            return;
        }

        await ExecuteAsync(async () =>
        {
            var nuevoMed = new Medicine
            {
                Name = nombre,
                Description = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion,
                StockQuantity = stock,
                Unit = "unidades"
            };
            
            var result = await ApiService.CrearMedicamentoAsync(nuevoMed);

            if (result.Success)
            {
                await ShowSuccessAsync("Medicamento creado");
                await LoadMedicamentosAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al crear medicamento");
            }
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadMedicamentosAsync();
    }
}
