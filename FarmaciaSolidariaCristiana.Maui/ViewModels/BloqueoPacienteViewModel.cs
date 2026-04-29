using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;
using System.Collections.ObjectModel;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

public partial class BloqueoPacienteViewModel : BaseViewModel
{
    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<PatientAutoCompleteItem> searchResults = new();

    [ObservableProperty]
    private bool isSearchResultsVisible;

    [ObservableProperty]
    private Patient? selectedPatient;

    [ObservableProperty]
    private bool hasSelectedPatient;

    [ObservableProperty]
    private bool isPatientBlocked;

    [ObservableProperty]
    private string blockDescription = string.Empty;

    private CancellationTokenSource? _searchCts;

    public BloqueoPacienteViewModel(IApiService apiService, IAuthService authService)
        : base(authService, apiService)
    {
        Title = "Bloqueo por Préstamo";
    }

    public async Task InitializeAsync()
    {
        var userInfo = await AuthService.GetUserInfoAsync();
        if (userInfo?.Role != Constants.RoleAdmin && userInfo?.Role != Constants.RoleFarmaceutico)
        {
            await ShowErrorAsync("No tiene permisos para acceder a esta funcionalidad.");
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            SearchResults.Clear();
            IsSearchResultsVisible = false;
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                await PerformSearchAsync(value, token);
            }
            catch (OperationCanceledException) { }
        }, token);
    }

    private async Task PerformSearchAsync(string q, CancellationToken token)
    {
        try
        {
            var response = await ApiService.SearchPacientesAutocompleteAsync(q);
            if (token.IsCancellationRequested) return;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                SearchResults.Clear();
                if (response.Success && response.Data != null)
                {
                    foreach (var item in response.Data)
                        SearchResults.Add(item);
                }
                IsSearchResultsVisible = SearchResults.Count > 0;
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[BloqueoPacienteVM] Search error: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task SelectPatientAsync(PatientAutoCompleteItem item)
    {
        if (item == null) return;

        IsSearchResultsVisible = false;
        SearchText = item.DisplayLabel;

        try
        {
            IsBusy = true;
            var response = await ApiService.GetPacienteAsync(item.Id);
            if (response.Success && response.Data != null)
            {
                SelectedPatient = response.Data;
                IsPatientBlocked = SelectedPatient.IsBlockedByLoan;
                HasSelectedPatient = true;
                BlockDescription = string.Empty;
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al cargar el paciente.");
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
    private async Task BlockPatientAsync()
    {
        if (SelectedPatient == null) return;

        if (string.IsNullOrWhiteSpace(BlockDescription))
        {
            await ShowErrorAsync("Debe ingresar la descripción del insumo en préstamo.");
            return;
        }

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Confirmar bloqueo",
            $"¿Bloquear a {SelectedPatient.FullName} por préstamo del insumo:\n\"{BlockDescription}\"?",
            "Sí, bloquear", "Cancelar");

        if (!confirmed) return;

        try
        {
            IsBusy = true;
            var response = await ApiService.BloquearPacientePrestamoAsync(SelectedPatient.Id, BlockDescription);
            if (response.Success && response.Data != null)
            {
                SelectedPatient = response.Data;
                IsPatientBlocked = true;
                BlockDescription = string.Empty;
                await Shell.Current.DisplayAlertAsync("Listo",
                    $"El paciente {SelectedPatient.FullName} fue bloqueado correctamente.", "Aceptar");
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al bloquear el paciente.");
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
    private async Task UnblockPatientAsync()
    {
        if (SelectedPatient == null) return;

        bool confirmed = await Shell.Current.DisplayAlertAsync(
            "Confirmar desbloqueo",
            $"¿Confirma que {SelectedPatient.FullName} devolvió el insumo y desea desbloquearlo?",
            "Sí, desbloquear", "Cancelar");

        if (!confirmed) return;

        try
        {
            IsBusy = true;
            var response = await ApiService.DesbloquearPacientePrestamoAsync(SelectedPatient.Id);
            if (response.Success && response.Data != null)
            {
                SelectedPatient = response.Data;
                IsPatientBlocked = false;
                await Shell.Current.DisplayAlertAsync("Listo",
                    $"El paciente {SelectedPatient.FullName} fue desbloqueado correctamente.", "Aceptar");
            }
            else
            {
                await ShowErrorAsync(response.Message ?? "Error al desbloquear el paciente.");
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
    private void ClearPatient()
    {
        SelectedPatient = null;
        HasSelectedPatient = false;
        IsPatientBlocked = false;
        BlockDescription = string.Empty;
        SearchText = string.Empty;
        SearchResults.Clear();
        IsSearchResultsVisible = false;
    }
}
