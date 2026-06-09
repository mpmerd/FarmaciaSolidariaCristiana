using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Models;
using FarmaciaSolidariaCristiana.Maui.Services;

namespace FarmaciaSolidariaCristiana.Maui.ViewModels;

/// <summary>
/// ViewModel para el Dashboard
/// </summary>
public partial class DashboardViewModel : BaseViewModel
{
    [ObservableProperty]
    private string _welcomeMessage = string.Empty;

    [ObservableProperty]
    private string _userRole = string.Empty;

    [ObservableProperty]
    private int _turnosPendientes;

    [ObservableProperty]
    private int _medicamentosDisponibles;

    [ObservableProperty]
    private int _insumosDisponibles;

    [ObservableProperty]
    private int _entregasHoy;

    [ObservableProperty]
    private bool _canManageTurnos;

    [ObservableProperty]
    private bool _canViewReports;

    [ObservableProperty]
    private bool _isRefreshingInBackground;

    // Decoración del Navbar
    [ObservableProperty]
    private bool _hasDecoration;

    [ObservableProperty]
    private string _decorationText = string.Empty;

    [ObservableProperty]
    private Color _decorationTextColor = Colors.White;

    [ObservableProperty]
    private string _decorationIcon = string.Empty;

    [ObservableProperty]
    private string _decorationImageUrl = string.Empty;

    [ObservableProperty]
    private bool _decorationIsPredefined;

    [ObservableProperty]
    private bool _decorationIsCustom;

    public bool IsDataLoaded { get; private set; }

    public DashboardViewModel(IAuthService authService, IApiService apiService) 
        : base(authService, apiService)
    {
        Title = "Inicio";
    }

    [RelayCommand]
    public async Task LoadDataAsync()
    {
        await ExecuteAsync(async () =>
        {
            // Cargar información del usuario
            var user = await AuthService.GetCurrentUserAsync();
            if (user != null)
            {
                WelcomeMessage = $"¡Hola, {user.UserName}!";
                UserRole = GetRoleDisplayName(user.Roles.FirstOrDefault() ?? "");
                
                // Permisos basados en rol
                CanManageTurnos = await AuthService.IsInAnyRoleAsync(
                    Constants.RoleAdmin, Constants.RoleFarmaceutico);
                CanViewReports = await AuthService.IsInAnyRoleAsync(
                    Constants.RoleAdmin, Constants.RoleViewer);
            }

            // Cargar estadísticas y decoración en paralelo
            await Task.WhenAll(LoadStatisticsAsync(), LoadDecorationAsync());
            IsDataLoaded = true;
        });
    }

    public async Task RefreshInBackgroundAsync()
    {
        if (IsRefreshingInBackground) return;
        try
        {
            IsRefreshingInBackground = true;
            await LoadStatisticsAsync();
        }
        catch
        {
            // Actualización silenciosa — no interrumpir al usuario
        }
        finally
        {
            IsRefreshingInBackground = false;
        }
    }

    private async Task LoadStatisticsAsync()
    {
        // Iniciar todas las llamadas en paralelo
        var turnosTask = CanManageTurnos
            ? ApiService.GetTurnosAsync()
            : ApiService.GetMisTurnosAsync();
        var medsTask = ApiService.GetMedicamentosAsync();
        var suppliesTask = ApiService.GetInsumosAsync();

        if (CanManageTurnos)
        {
            var deliveriesTask = ApiService.GetEntregasAsync();
            await Task.WhenAll(turnosTask, medsTask, suppliesTask, deliveriesTask);

            var deliveriesResult = await deliveriesTask;
            if (deliveriesResult.Success && deliveriesResult.Data != null)
                EntregasHoy = deliveriesResult.Data.Count(d => d.DeliveryDate.Date == DateTime.Today);
        }
        else
        {
            await Task.WhenAll(turnosTask, medsTask, suppliesTask);
        }

        var turnosResult = await turnosTask;
        if (turnosResult.Success && turnosResult.Data != null)
            TurnosPendientes = turnosResult.Data.Count(t => t.Estado == "Pendiente");

        var medsResult = await medsTask;
        if (medsResult.Success && medsResult.Data != null)
            MedicamentosDisponibles = medsResult.Data.Count(m => m.StockQuantity > 0);

        var suppliesResult = await suppliesTask;
        if (suppliesResult.Success && suppliesResult.Data != null)
            InsumosDisponibles = suppliesResult.Data.Count(s => s.StockQuantity > 0);
    }

    private static string GetRoleDisplayName(string role)
    {
        return role.ToLower() switch
        {
            "admin" => "Administrador",
            "farmaceutico" => "Farmacéutico",
            "viewer" => "Visualizador",
            "viewerpublic" => "Paciente",
            _ => "Usuario"
        };
    }

    private async Task LoadDecorationAsync()
    {
        try
        {
            var dto = await ApiService.GetNavbarDecorationAsync();
            if (dto == null || !dto.Active || string.IsNullOrWhiteSpace(dto.DisplayText))
            {
                HasDecoration = false;
                return;
            }

            DecorationText = dto.DisplayText;

            // Color del texto (fallback blanco semitransparente para que quede bien sobre el fondo azul)
            DecorationTextColor = TryParseColor(dto.TextColor, Color.FromArgb("#CCFFFFFF"));

            // Tipo de decoración
            var isCustom = string.Equals(dto.Type, "Custom", StringComparison.OrdinalIgnoreCase)
                           && !string.IsNullOrWhiteSpace(dto.CustomIconPath);

            DecorationIsCustom = isCustom;
            DecorationIsPredefined = !isCustom;

            if (isCustom)
            {
                DecorationImageUrl = $"{Constants.ApiBaseUrl}{dto.CustomIconPath}";
                DecorationIcon = string.Empty;
            }
            else
            {
                DecorationIcon = MapIconClassToEmoji(dto.IconClass);
                DecorationImageUrl = string.Empty;
            }

            HasDecoration = true;
        }
        catch
        {
            HasDecoration = false;
        }
    }

    private static Color TryParseColor(string? hex, Color fallback)
    {
        if (string.IsNullOrWhiteSpace(hex)) return fallback;
        try { return Color.FromArgb(hex); }
        catch { return fallback; }
    }

    private static string MapIconClassToEmoji(string? iconClass) =>
        iconClass switch
        {
            var s when s != null && s.Contains("tree")           => "🎄",
            var s when s != null && s.Contains("star")           => "⭐",
            var s when s != null && s.Contains("cross")          => "✝️",
            var s when s != null && s.Contains("heart")          => "❤️",
            var s when s != null && s.Contains("fire")           => "🔥",
            _ => "✨"
        };

    [RelayCommand]
    private async Task NavigateToTurnosAsync()
    {
        await Shell.Current.GoToAsync("//TurnosPage");
    }

    [RelayCommand]
    private async Task NavigateToMedicamentosAsync()
    {
        await Shell.Current.GoToAsync("//MedicamentosPage");
    }

    [RelayCommand]
    private async Task NavigateToInsumosAsync()
    {
        await Shell.Current.GoToAsync("//InsumosPage");
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        ApiService.InvalidateDashboardCache();
        await LoadDataAsync();
    }
}
