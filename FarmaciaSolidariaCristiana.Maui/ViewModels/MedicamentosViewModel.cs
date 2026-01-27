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
        
        var action = await Shell.Current.DisplayActionSheet(
            medicamento.Name,
            "Cancelar",
            null,
            CanEdit ? new[] { "Ver detalles", "Editar", "Ajustar stock" } : new[] { "Ver detalles" });

        switch (action)
        {
            case "Ver detalles":
                await ShowMedicamentoDetailsAsync(medicamento);
                break;
            case "Editar":
                await NavigateToAsync($"medicamentoedit?id={medicamento.Id}");
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

        await NavigateToAsync("medicamentoedit");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        await LoadMedicamentosAsync();
    }
}
