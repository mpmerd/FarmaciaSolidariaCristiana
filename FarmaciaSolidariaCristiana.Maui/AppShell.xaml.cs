using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Services;
using FarmaciaSolidariaCristiana.Maui.Views;
using Microsoft.Extensions.DependencyInjection;

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
        Routing.RegisterRoute("BloqueoPacientePage", typeof(BloqueoPacientePage));
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

            // Fase 1.4: arrancar notificaciones en el path de token guardado (auto-login).
            // LoginViewModel ya arranca en el path de login manual; aquí cubrimos el cold-start.
            await StartNotificationsOnAutoLoginAsync();
        }
        else
        {
            HideAllRoleMenus();
            await GoToAsync("//LoginPage");
        }
    }

    /// <summary>
    /// Inicia push + polling cuando el usuario entra por auto-login (token guardado).
    /// Idempotente: si el polling ya está corriendo (vínimos de login manual), no hace nada.
    /// Usa <see cref="App.Services"/> (static) como fuente robusta: MauiContext.Services puede no
    /// estar listo en cold-start y hacía abortar polling+FG (bug: "Notificaciones activas" no
    /// aparecía en auto-login hasta borrar datos). Arranca primero los canales que solo necesitan
    /// el token local (ya validado por IsAuthenticatedAsync); GetUserInfo/push-reg va después y
    /// no bloquea.
    /// </summary>
    private async Task StartNotificationsOnAutoLoginAsync()
    {
        try
        {
            // App.Services se setea en el ctor de App (siempre disponible tras el arranque).
            // Evita el early-return que ocurría cuando MauiContext.Services no estaba listo en ~1s.
            var services = App.Services;
            if (services == null)
            {
                for (int i = 0; i < 10; i++)
                {
                    services = Application.Current?.Handler?.MauiContext?.Services;
                    if (services != null) break;
                    await Task.Delay(100);
                }
            }

            if (services == null)
            {
                AppLog.Error("[AppShell] No se pudo resolver ServiceProvider para notificaciones en auto-login");
                return;
            }

            var polling = services.GetService<IPollingNotificationService>();
            var notif = services.GetService<INotificationService>();

            // Si el polling ya corre, venimos de login manual -> nada que hacer aquí.
            if (polling != null && polling.IsRunning)
            {
                AppLog.Info("[AppShell] Polling ya activo (login manual), auto-login no reinicia");
                return;
            }

            // 1) Arrancar primero los canales que SOLO necesitan el token local (ya validado
            //    por IsAuthenticatedAsync). Desacopla el push real del GetUserInfo/push-reg,
            //    que antes abortaban todo el arranque si fallaban.
            if (Constants.SignalRChannelEnabled)
            {
#if ANDROID
                try
                {
                    Platforms.Android.NotificationsForegroundStarter.Start();
                    AppLog.Info("[AppShell] Foreground Service start solicitado en auto-login");
                }
                catch (Exception ex)
                {
                    AppLog.Error($"[AppShell] Error iniciando Foreground Service en auto-login: {ex.Message}");
                }
#endif
            }

            if (polling != null)
            {
                try
                {
                    await polling.StartAsync();
                    AppLog.Info("[AppShell] Polling iniciado en auto-login");
                }
                catch (Exception ex)
                {
                    AppLog.Error($"[AppShell] Error iniciando polling en auto-login: {ex.Message}");
                }
            }

            // 2) Registro push + tags: no bloquea los canales ya arrancados. Si falla, no afecta.
            var userInfo = await _authService.GetUserInfoAsync();
            if (notif != null && userInfo != null)
            {
                var role = (userInfo.Roles != null && userInfo.Roles.Count > 0) ? userInfo.Roles[0] : "user";
                try
                {
                    await notif.SetUserTagsAsync(userInfo.Id, role);
                    await notif.RegisterDeviceAsync();
                    AppLog.Info("[AppShell] Push registrado en auto-login");
                }
                catch (Exception ex)
                {
                    AppLog.Warn($"[AppShell] Error registrando push en auto-login: {ex.Message}");
                }
            }
            else
            {
                AppLog.Warn("[AppShell] GetUserInfo null en auto-login (push-reg omitido; canales ya arrancados)");
            }
        }
        catch (Exception ex)
        {
            AppLog.Error($"[AppShell] Error en StartNotificationsOnAutoLoginAsync: {ex.Message}");
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

        // Bloqueos - Farmaceutico, Admin only
        FlyoutBloqueos.IsVisible = isFarmaceutico || isAdmin;
        
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
        FlyoutBloqueos.IsVisible = false;
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
            // Fase 1.5: detener polling y desregistrar push antes de borrar credenciales.
            await StopNotificationsOnLogoutAsync();

#if ANDROID
            // Fase 2: detener Foreground Service (cierra la conexión SignalR).
            if (Constants.SignalRChannelEnabled)
            {
                try { Platforms.Android.NotificationsForegroundStarter.Stop(); }
                catch (Exception ex) { AppLog.Info($"[AppShell] Error deteniendo Foreground Service: {ex.Message}"); }
            }
#endif

            await _authService.LogoutAsync();
            HideAllRoleMenus();
            await GoToAsync("//LoginPage");
        }
    }

    /// <summary>
    /// Detiene el polling y desregistra el dispositivo al cerrar sesión.
    /// Evita que el polling siga corriendo contra un token invalidado.
    /// </summary>
    private async Task StopNotificationsOnLogoutAsync()
    {
        try
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services == null) return;

            var polling = services.GetService<IPollingNotificationService>();
            if (polling != null)
            {
                try
                {
                    await polling.StopAsync();
                    AppLog.Info("[AppShell] Polling detenido en logout");
                }
                catch (Exception ex)
                {
                    AppLog.Info($"[AppShell] Error deteniendo polling en logout: {ex.Message}");
                }
            }

            var notif = services.GetService<INotificationService>();
            if (notif != null)
            {
                try
                {
                    await notif.UnregisterUserAsync();
                    AppLog.Info("[AppShell] Dispositivo desregistrado en logout");
                }
                catch (Exception ex)
                {
                    AppLog.Info($"[AppShell] Error desregistrando en logout: {ex.Message}");
                }
            }

            var pushHealth = services.GetService<IPushHealthService>();
            pushHealth?.Reset();
        }
        catch (Exception ex)
        {
            AppLog.Info($"[AppShell] Error en StopNotificationsOnLogoutAsync: {ex.Message}");
        }
    }
}
