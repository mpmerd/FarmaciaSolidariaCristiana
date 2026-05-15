using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using FarmaciaSolidariaCristiana.Maui.Messages;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class ReprogramarTurnosViewModel : BaseViewModel
{
    [ObservableProperty]
    private DateTime fechaSeleccionada = DateTime.Today;

    [ObservableProperty]
    private string motivo = string.Empty;

    [ObservableProperty]
    private int turnosAfectados;

    [ObservableProperty]
    private ObservableCollection<TurnoAfectadoDto> turnosEnFecha = new();

    [ObservableProperty]
    private bool hasPreview;

    [ObservableProperty]
    private ReprogramarResultDto? lastResult;

    [ObservableProperty]
    private bool showResult;

    public ReprogramarTurnosViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Reprogramar Turnos";
    }

    partial void OnFechaSeleccionadaChanged(DateTime value)
    {
        // Limpiar preview cuando cambia la fecha
        HasPreview = false;
        TurnosEnFecha.Clear();
        TurnosAfectados = 0;
    }

    [RelayCommand]
    private async Task LoadPreviewAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ShowResult = false;

            var response = await ApiService.GetReprogramarPreviewAsync(FechaSeleccionada);

            if (response.Success && response.Data != null)
            {
                TurnosAfectados = response.Data.TotalTurnos;
                TurnosEnFecha.Clear();
                
                foreach (var turno in response.Data.Turnos)
                {
                    TurnosEnFecha.Add(turno);
                }

                HasPreview = true;

                if (TurnosAfectados == 0)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Sin turnos",
                        $"No hay turnos pendientes o aprobados para el {FechaSeleccionada:dd/MM/yyyy}",
                        "OK");
                }
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al obtener preview");
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
    private async Task ReprogramarAsync()
    {
        if (IsBusy) return;

        if (string.IsNullOrWhiteSpace(Motivo))
        {
            await ShowErrorAsync("Debe proporcionar un motivo para la reprogramación");
            return;
        }

        if (TurnosAfectados == 0)
        {
            await ShowErrorAsync("No hay turnos para reprogramar en esta fecha");
            return;
        }

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Confirmar Reprogramación",
            $"¿Está seguro de reprogramar {TurnosAfectados} turno(s) del {FechaSeleccionada:dd/MM/yyyy}?\n\n" +
            "Esta acción:\n" +
            "• Buscará nuevos slots disponibles (Martes/Jueves 1-4 PM)\n" +
            "• Actualizará las fechas de los turnos\n" +
            "• Registrará el cambio en los comentarios\n\n" +
            "Esta acción no se puede deshacer.",
            "Reprogramar",
            "Cancelar");

        if (!confirm) return;

        try
        {
            IsBusy = true;

            var response = await ApiService.ReprogramarTurnosAsync(FechaSeleccionada, Motivo);

            if (response.Success && response.Data != null)
            {
                LastResult = response.Data;
                ShowResult = true;

                string message = response.Data.Mensaje;

                if (response.Data.Reprogramados == 0)
                {
                    await Shell.Current.DisplayAlertAsync(
                        "Sin cambios",
                        "No se reprogramó ningún turno. Verifique que existan turnos aprobados en esa fecha.",
                        "OK");
                }
                else if (response.Data.NoReprogramados > 0)
                {
                    await Shell.Current.DisplayAlertAsync("Reprogramación Parcial", message, "OK");
                    WeakReferenceMessenger.Default.Send(new TurnosReprogramadosMessage { Reprogramados = response.Data.Reprogramados });
                }
                else
                {
                    await Shell.Current.DisplayAlertAsync("Éxito", message, "OK");
                    WeakReferenceMessenger.Default.Send(new TurnosReprogramadosMessage { Reprogramados = response.Data.Reprogramados });
                }

                // Limpiar y recargar preview
                Motivo = string.Empty;
                await LoadPreviewAsync();
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al reprogramar turnos");
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
    private void ClearResult()
    {
        ShowResult = false;
        LastResult = null;
    }
}
