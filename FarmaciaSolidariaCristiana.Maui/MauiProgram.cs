using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using FarmaciaSolidariaCristiana.Maui.Helpers;
using FarmaciaSolidariaCristiana.Maui.Services;
using FarmaciaSolidariaCristiana.Maui.ViewModels;
using FarmaciaSolidariaCristiana.Maui.Views;

namespace FarmaciaSolidariaCristiana.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Configure HttpClient
        builder.Services.AddSingleton(sp =>
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(Constants.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
            return client;
        });

        // Register Services
        builder.Services.AddSingleton<IAuthService, AuthService>();
        builder.Services.AddSingleton<IApiService, ApiService>();
        builder.Services.AddSingleton<INotificationService, NotificationService>();

        // Register ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<TurnosViewModel>();
        builder.Services.AddTransient<MedicamentosViewModel>();
        builder.Services.AddTransient<InsumosViewModel>();
        builder.Services.AddTransient<DonacionesViewModel>();
        builder.Services.AddTransient<EntregasViewModel>();
        builder.Services.AddTransient<PacientesViewModel>();
        builder.Services.AddTransient<PatrocinadoresViewModel>();
        builder.Services.AddTransient<ReportesViewModel>();
        builder.Services.AddTransient<FechasBloqueadasViewModel>();
        builder.Services.AddTransient<ProfileViewModel>();

        // Register Pages
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<TurnosPage>();
        builder.Services.AddTransient<MedicamentosPage>();
        builder.Services.AddTransient<InsumosPage>();
        builder.Services.AddTransient<DonacionesPage>();
        builder.Services.AddTransient<EntregasPage>();
        builder.Services.AddTransient<PacientesPage>();
        builder.Services.AddTransient<PatrocinadoresPage>();
        builder.Services.AddTransient<ReportesPage>();
        builder.Services.AddTransient<FechasBloqueadasPage>();
        builder.Services.AddTransient<ProfilePage>();
        
        // Register AppShell
        builder.Services.AddTransient<AppShell>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
