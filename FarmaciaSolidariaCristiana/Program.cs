using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using FarmaciaSolidariaCristiana.Data;
using FarmaciaSolidariaCristiana.Services;
using FarmaciaSolidariaCristiana.Filters;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// ========================================
// CONFIGURACIÓN DE LÍMITE DE TAMAÑO DE ARCHIVOS
// ========================================
// Configurar Kestrel para aceptar archivos más grandes (20MB)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 20 * 1024 * 1024; // 20MB
});

// Configurar cultura en español
var cultureInfo = new CultureInfo("es-ES");
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// ========================================
// CONFIGURACIÓN DE SEGURIDAD HTTPS/HSTS
// ========================================
// Configurar HSTS con opciones personalizadas
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365); // 1 año
    options.IncludeSubDomains = true;
    options.Preload = true;
});

// Configurar redirección HTTPS
builder.Services.AddHttpsRedirection(options =>
{
    options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
    options.HttpsPort = 443;
});

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Add global filters (solo para MVC, no para API)
    options.Filters.Add<MaintenanceModeFilter>();
});

// ========================================
// CONFIGURACIÓN DE JWT PARA API
// ========================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey no está configurada en appsettings.json");

builder.Services.AddAuthentication(options =>
{
    // Mantener cookies como default para MVC web
    options.DefaultScheme = IdentityConstants.ApplicationScheme;
    options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    // Requerir HTTPS para metadatos en producción
    options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"] ?? "FarmaciaSolidariaCristiana",
        ValidAudience = jwtSettings["Audience"] ?? "FarmaciaSolidariaCristianaApi",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero // Sin tolerancia de tiempo
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
                context.Response.Headers.Append("Token-Expired", "true");
            }
            return Task.CompletedTask;
        },
        // IMPORTANTE: Manejar el challenge para devolver 401 en lugar de redirigir
        OnChallenge = context =>
        {
            // Evitar la redirección por defecto
            context.HandleResponse();
            
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";
            
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                message = "No autorizado. Token JWT no proporcionado o inválido.",
                error = string.IsNullOrEmpty(context.ErrorDescription) 
                    ? "Unauthorized" 
                    : context.ErrorDescription
            });
            
            return context.Response.WriteAsync(result);
        },
        OnForbidden = context =>
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            
            var result = System.Text.Json.JsonSerializer.Serialize(new
            {
                success = false,
                message = "Acceso denegado. No tienes permisos para este recurso.",
                error = "Forbidden"
            });
            
            return context.Response.WriteAsync(result);
        }
    };
});

// Configure DbContext with SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Email Service
builder.Services.AddScoped<IEmailService, EmailService>();

// Register Image Compression Service
builder.Services.AddScoped<IImageCompressionService, ImageCompressionService>();

// Register Turno Service
builder.Services.AddScoped<ITurnoService, TurnoService>();

// Register Maintenance Service
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();

// Register OneSignal Notification Service for Push Notifications
// Solo registrar si la configuración de OneSignal está presente y válida
var oneSignalAppId = builder.Configuration["OneSignalSettings:AppId"];
var oneSignalApiKey = builder.Configuration["OneSignalSettings:RestApiKey"];
if (!string.IsNullOrEmpty(oneSignalAppId) && 
    !string.IsNullOrEmpty(oneSignalApiKey) &&
    !oneSignalAppId.StartsWith("TU_") && 
    !oneSignalApiKey.StartsWith("TU_"))
{
    builder.Services.AddScoped<IOneSignalNotificationService, OneSignalNotificationService>();
}
else
{
    // Registrar una implementación nula/vacía para evitar errores de DI
    builder.Services.AddScoped<IOneSignalNotificationService, NullOneSignalNotificationService>();
}

// Servicio de notificaciones pendientes (polling - funciona sin push)
builder.Services.AddScoped<IPendingNotificationService, PendingNotificationService>();

// Register Background Services
builder.Services.AddHostedService<TurnoCleanupService>();

// Configure Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password settings (less restrictive for local use)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 6;
    
    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    
    // User settings
    options.User.RequireUniqueEmail = true;
    
    // Token lifespan for password reset (24 hours)
    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultProvider;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure token lifespan
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(24);
});

// Configure cookie settings for login
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
    
    // Configuración para compatibilidad con Safari y dispositivos móviles
    options.Cookie.SameSite = SameSiteMode.Lax;
    // SEGURIDAD: SameAsRequest para compatibilidad con proxy reverso (Somee.com)
    // El servidor hace HTTPS redirection, pero las cookies deben funcionar
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".FarmaciaSolidaria.Auth";
});

// Add HttpClient for CIMA API calls with configuration
builder.Services.AddHttpClient("CimaApi", client =>
{
    client.BaseAddress = new Uri("https://cima.aemps.es/");
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "FarmaciaSolidariaCristiana/1.0");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
});

// Also add default HttpClient
builder.Services.AddHttpClient();

var app = builder.Build();

// Initialize database and seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await DbInitializer.Initialize(services);
        
        // Seed test data (only if database is empty)
        var context = services.GetRequiredService<ApplicationDbContext>();
        await DataSeeder.SeedTestData(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    
    // Enable HSTS for production (tells browsers to only use HTTPS)
    // MaxAge de 1 año, incluir subdominios y preload
    app.UseHsts();
}

// Force HTTPS redirection - SIEMPRE en producción
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Middleware HSTS personalizado deshabilitado - UseHsts() ya lo maneja
/*
app.Use(async (context, next) =>
{
    if (!app.Environment.IsDevelopment())
    {
        // Forzar HTTPS con encabezado Strict-Transport-Security
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains; preload");
    }
    await next();
});
*/

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// Mapear controladores API (rutas bajo /api/*)
app.MapControllers();

// Mapear rutas MVC tradicionales
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
