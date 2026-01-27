using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class InsumosViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<Supply> insumos = new();

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private bool canEdit;

    [ObservableProperty]
    private bool isRefreshing;

    private List<Supply> _allInsumos = new();

    public InsumosViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Insumos";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await AuthService.GetUserInfoAsync();
        CanEdit = userInfo?.Role == Constants.RoleAdmin || 
                  userInfo?.Role == Constants.RoleFarmaceutico;
        await LoadInsumosAsync();
    }

    [RelayCommand]
    private async Task LoadInsumosAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await ApiService.GetInsumosAsync();
            if (response.Success && response.Data != null)
            {
                _allInsumos = response.Data;
                ApplyFilter();
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar insumos");
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
        await LoadInsumosAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            Insumos = new ObservableCollection<Supply>(_allInsumos);
        }
        else
        {
            var filtered = _allInsumos
                .Where(i => i.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                           (i.Description?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
            Insumos = new ObservableCollection<Supply>(filtered);
        }
    }

    [RelayCommand]
    private async Task SelectInsumoAsync(Supply insumo)
    {
        if (insumo == null) return;
        
        var options = CanEdit 
            ? new[] { "Ver detalles", "Editar", "Ajustar stock", "Eliminar" } 
            : new[] { "Ver detalles" };
        
        var action = await Shell.Current.DisplayActionSheet(
            insumo.Name,
            "Cancelar",
            null,
            options);

        switch (action)
        {
            case "Ver detalles":
                await ShowInsumoDetailsAsync(insumo);
                break;
            case "Editar":
                await EditInsumoAsync(insumo);
                break;
            case "Ajustar stock":
                await AjustarStockAsync(insumo);
                break;
            case "Eliminar":
                await DeleteInsumoAsync(insumo);
                break;
        }
    }

    private async Task ShowInsumoDetailsAsync(Supply insumo)
    {
        var details = $"Nombre: {insumo.Name}\n" +
                      $"Descripción: {insumo.Description ?? "N/A"}\n" +
                      $"Stock: {insumo.StockQuantity} {insumo.Unit}\n" +
                      $"Estado: {insumo.StockStatus}";

        await Shell.Current.DisplayAlert("Detalles del Insumo", details, "Cerrar");
    }

    private async Task AjustarStockAsync(Supply insumo)
    {
        var input = await Shell.Current.DisplayPromptAsync(
            "Ajustar Stock",
            $"Stock actual: {insumo.StockQuantity}\nIngrese el nuevo stock:",
            placeholder: insumo.StockQuantity.ToString(),
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrEmpty(input)) return;

        if (!int.TryParse(input, out var newStock) || newStock < 0)
        {
            await ShowErrorAsync("Stock inválido");
            return;
        }

        try
        {
            IsBusy = true;
            insumo.StockQuantity = newStock;
            var result = await ApiService.ActualizarInsumoAsync(insumo);

            if (result.Success)
            {
                await Shell.Current.DisplayAlert("Éxito", "Stock actualizado", "OK");
                await LoadInsumosAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al actualizar stock");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AddInsumoAsync()
    {
        if (!CanEdit)
        {
            await ShowErrorAsync("No tiene permisos para crear insumos.");
            return;
        }
        
        // Nombre del nuevo insumo
        var nombre = await Shell.Current.DisplayPromptAsync(
            "Nuevo Insumo",
            "Nombre del insumo:",
            maxLength: 200);

        if (string.IsNullOrWhiteSpace(nombre)) return;

        // Descripción
        var descripcion = await Shell.Current.DisplayPromptAsync(
            "Nuevo Insumo",
            "Descripción (opcional):",
            maxLength: 500);

        if (descripcion == null) return;

        // Stock inicial
        var stockStr = await Shell.Current.DisplayPromptAsync(
            "Nuevo Insumo",
            "Stock inicial:",
            placeholder: "0",
            keyboard: Keyboard.Numeric);

        if (string.IsNullOrEmpty(stockStr)) return;
        if (!int.TryParse(stockStr, out var stock) || stock < 0)
        {
            await ShowErrorAsync("Stock inválido");
            return;
        }

        try
        {
            IsBusy = true;
            var nuevoInsumo = new Supply
            {
                Name = nombre,
                Description = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion,
                StockQuantity = stock,
                Unit = "unidades"
            };
            
            var result = await ApiService.CrearInsumoAsync(nuevoInsumo);

            if (result.Success)
            {
                await Shell.Current.DisplayAlert("Éxito", "Insumo creado", "OK");
                await LoadInsumosAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al crear insumo");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task EditInsumoAsync(Supply insumo)
    {
        if (!CanEdit || insumo == null)
        {
            await ShowErrorAsync("No tiene permisos para editar insumos.");
            return;
        }
        
        // Editar nombre
        var nuevoNombre = await Shell.Current.DisplayPromptAsync(
            "Editar Insumo",
            "Nombre del insumo:",
            initialValue: insumo.Name,
            maxLength: 200);

        if (string.IsNullOrEmpty(nuevoNombre)) return;

        // Editar descripción
        var nuevaDescripcion = await Shell.Current.DisplayPromptAsync(
            "Editar Insumo",
            "Descripción:",
            initialValue: insumo.Description ?? "",
            maxLength: 500);

        if (nuevaDescripcion == null) return;

        try
        {
            IsBusy = true;
            insumo.Name = nuevoNombre;
            insumo.Description = string.IsNullOrWhiteSpace(nuevaDescripcion) ? null : nuevaDescripcion;
            
            var result = await ApiService.ActualizarInsumoAsync(insumo);

            if (result.Success)
            {
                await Shell.Current.DisplayAlert("Éxito", "Insumo actualizado", "OK");
                await LoadInsumosAsync();
            }
            else
            {
                await ShowErrorAsync(result.Message ?? "Error al actualizar insumo");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteInsumoAsync(Supply insumo)
    {
        if (!CanEdit || insumo == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Eliminar Insumo",
            $"¿Estás seguro de eliminar '{insumo.Name}'?",
            "Sí, eliminar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                IsBusy = true;
                var response = await ApiService.DeleteInsumoAsync(insumo.Id);
                
                if (response.Success)
                {
                    _allInsumos.Remove(insumo);
                    ApplyFilter();
                    await Shell.Current.DisplayAlert("Éxito", "Insumo eliminado correctamente", "OK");
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
