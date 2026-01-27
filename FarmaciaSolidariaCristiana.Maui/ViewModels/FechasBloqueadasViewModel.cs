using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class FechasBloqueadasViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<FechaBloqueadaDto> fechasBloqueadas = new();

    [ObservableProperty]
    private bool isRefreshing;

    [ObservableProperty]
    private DateTime nuevaFecha = DateTime.Today.AddDays(1);

    [ObservableProperty]
    private string motivo = string.Empty;

    public FechasBloqueadasViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Fechas Bloqueadas";
    }

    public async Task InitializeAsync()
    {
        await LoadFechasAsync();
    }

    [RelayCommand]
    private async Task LoadFechasAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            IsRefreshing = true;

            var response = await ApiService.GetFechasBloqueadasAsync();
            if (response.Success && response.Data != null)
            {
                FechasBloqueadas = new ObservableCollection<FechaBloqueadaDto>(
                    response.Data.OrderByDescending(f => f.Fecha));
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar fechas bloqueadas");
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
        await LoadFechasAsync();
    }

    [RelayCommand]
    private async Task AddFechaBloqueadaAsync()
    {
        if (NuevaFecha <= DateTime.Today)
        {
            await ShowErrorAsync("La fecha debe ser posterior a hoy");
            return;
        }

        try
        {
            IsBusy = true;

            var request = new
            {
                Fecha = NuevaFecha.ToString("yyyy-MM-dd"),
                Motivo = string.IsNullOrWhiteSpace(Motivo) ? "Fecha bloqueada" : Motivo
            };

            var response = await ApiService.CreateFechaBloqueadaAsync(request);
            
            if (response.Success)
            {
                Motivo = string.Empty;
                NuevaFecha = DateTime.Today.AddDays(1);
                await LoadFechasAsync();
                await Shell.Current.DisplayAlert("Éxito", "Fecha bloqueada agregada correctamente", "OK");
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al agregar fecha");
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
    private async Task DeleteFechaBloqueadaAsync(FechaBloqueadaDto fecha)
    {
        if (fecha == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Eliminar Fecha",
            $"¿Estás seguro de eliminar el bloqueo del {fecha.Fecha:dd/MM/yyyy}?",
            "Sí, eliminar",
            "Cancelar");

        if (confirm)
        {
            try
            {
                IsBusy = true;
                var response = await ApiService.DeleteFechaBloqueadaAsync(fecha.Id);
                
                if (response.Success)
                {
                    FechasBloqueadas.Remove(fecha);
                    await Shell.Current.DisplayAlert("Éxito", "Fecha desbloqueada correctamente", "OK");
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
