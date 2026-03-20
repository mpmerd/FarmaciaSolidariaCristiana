using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Services;
using FarmaciaSolidariaCristiana.Maui.Views;

namespace FarmaciaSolidariaCristiana.Maui;

public partial class AppShell : Shell
{
    private readonly IAuthService _authService;
    
    public AppShell(IAuthService authService)
    {
        InitializeComponent();
        _authService = authService;
        
        // Aplicar tema al Flyout
        ApplyFlyoutTheme();
        
        // Register routes for navigation
        RegisterRoutes();
        
        // Check authentication status on startup
        CheckAuthenticationAsync();
    }
    
    private void ApplyFlyoutTheme()
    {
        // Aplicar color de fondo según el tema
        if (Application.Current?.RequestedTheme == AppTheme.Dark)
        {
            this.FlyoutBackgroundColor = Color.FromArgb("#1a1a1a");
        }
        else
        {
            this.FlyoutBackgroundColor = Colors.White;
        }
    }
    
    private void RegisterRoutes()
    {
        Routing.RegisterRoute("MaintenancePage", typeof(MaintenancePage));
        Routing.RegisterRoute("LoginPage", typeof(LoginPage));
        Routing.RegisterRoute(nameof(RegisterPage), typeof(RegisterPage));
        Routing.RegisterRoute("DashboardPage", typeof(DashboardPage));
        Routing.RegisterRoute("TurnosPage", typeof(TurnosPage));
        Routing.RegisterRoute("SolicitarTurnoPage", typeof(SolicitarTurnoPage));
        Routing.RegisterRoute("MedicamentosPage", typeof(MedicamentosPage));
        Routing.RegisterRoute("InsumosPage", typeof(InsumosPage));
        Routing.RegisterRoute("DonacionesPage", typeof(DonacionesPage));
        Routing.RegisterRoute("EntregasPage", typeof(EntregasPage));
        Routing.RegisterRoute("nueva-entrega", typeof(NuevaEntregaPage));
        Routing.RegisterRoute("nueva-donacion", typeof(NuevaDonacionPage));
        Routing.RegisterRoute("PacientesPage", typeof(PacientesPage));
        Routing.RegisterRoute("PatrocinadoresPage", typeof(PatrocinadoresPage));
        Routing.RegisterRoute("ReportesPage", typeof(ReportesPage));
        Routing.RegisterRoute("FechasBloqueadasPage", typeof(FechasBloqueadasPage));
        Routing.RegisterRoute("ReprogramarTurnosPage", typeof(ReprogramarTurnosPage));
        Routing.RegisterRoute("UsuariosPage", typeof(UsuariosPage));
        Routing.RegisterRoute("BroadcastPage", typeof(BroadcastPage));
        Routing.RegisterRoute("AboutPage", typeof(AboutPage));
        Routing.RegisterRoute("ProfilePage", typeof(ProfilePage));
        Routing.RegisterRoute("ChangePasswordPage", typeof(ChangePasswordPage));
    }
    
    private async void CheckAuthenticationAsync()
    {
        var isAuthenticated = await _authService.IsAuthenticatedAsync();
        
        if (isAuthenticated)
        {
            await UpdateMenuForRoleAsync();
            await GoToAsync("//DashboardPage");
        }
        else
        {
            HideAllRoleMenus();
            await GoToAsync("//LoginPage");
        }
    }
    
    public async Task UpdateMenuForRoleAsync()
    {
        var userInfo = await _authService.GetUserInfoAsync();
        var role = userInfo?.Role ?? "";
        
        // Update header
        LblUserName.Text = userInfo?.UserName ?? "Usuario";
        LblUserRole.Text = GetRoleDisplayName(role);
        LblAppVersion.Text = $"v{AppInfo.VersionString}";
        
        // Configure menu visibility based on role
        ConfigureMenuForRole(role);
    }
    
    private void ConfigureMenuForRole(string role)
    {
        // ViewerPublic (Paciente): Dashboard, Mis Turnos, Medicamentos, Insumos, Donaciones, Entregas, Patrocinadores
        // Viewer: + Pacientes, Reportes
        // Farmaceutico: + Gestión completa
        // Admin: + Avanzado
        
        bool isViewer = role == Constants.RoleViewer;
        bool isFarmaceutico = role == Constants.RoleFarmaceutico;
        bool isAdmin = role == Constants.RoleAdmin;
        bool isViewerOrHigher = isViewer || isFarmaceutico || isAdmin;
        
        // Pacientes - Viewer, Farmaceutico, Admin
        FlyoutPacientes.IsVisible = isViewerOrHigher;
        
        // Reportes - Viewer, Farmaceutico, Admin  
        FlyoutReportes.IsVisible = isViewerOrHigher;
        
        // Avanzado - Admin only
        FlyoutAvanzado.IsVisible = isAdmin;
        
        // Update Turnos title based on role
        if (role == Constants.RoleViewerPublic)
        {
            FlyoutTurnos.Title = "Mis Turnos";
        }
        else
        {
            FlyoutTurnos.Title = "Gestión Turnos";
        }
    }
    
    private void HideAllRoleMenus()
    {
        FlyoutPacientes.IsVisible = false;
        FlyoutReportes.IsVisible = false;
        FlyoutAvanzado.IsVisible = false;
    }
    
    private string GetRoleDisplayName(string role)
    {
        return role switch
        {
            Constants.RoleAdmin => "Administrador",
            Constants.RoleFarmaceutico => "Farmacéutico",
            Constants.RoleViewer => "Visualizador",
            Constants.RoleViewerPublic => "Paciente",
            _ => "Usuario"
        };
    }
    
    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        bool confirm = await DisplayAlertAsync(
            "Cerrar Sesión", 
            "¿Estás seguro que deseas cerrar sesión?", 
            "Sí", 
            "No");
            
        if (confirm)
        {
            await _authService.LogoutAsync();
            HideAllRoleMenus();
            await GoToAsync("//LoginPage");
        }
    }
}
