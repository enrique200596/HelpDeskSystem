using HelpDeskSystem.Data;
using HelpDeskSystem.Web.Components;
using HelpDeskSystem.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using HelpDeskSystem.Web.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONFIGURACIÓN DE INFRAESTRUCTURA (DATOS)
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no fue encontrada.");

// Registro de la Fábrica de Contexto (Único método seguro para Blazor Server)
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

// CORRECCIÓN CRÍTICA: Se elimina el registro 'AddScoped<AppDbContext>'. 
// En Blazor Server, inyectar el contexto directamente en servicios Scoped causa bloqueos de hilos.

// ============================================================
// 2. SEGURIDAD Y AUTENTICACIÓN
// ============================================================
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/error";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// Necesario para soportar controladores de login y validación de tokens
builder.Services.AddControllersWithViews();
builder.Services.AddAntiforgery();

builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// ============================================================
// 3. SERVICIOS DE APLICACIÓN
// ============================================================
builder.Services.AddSingleton<TicketStateContainer>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IManualService, ManualService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// ============================================================
// 4. PROTECCIÓN DE DATOS Y PERSISTENCIA
// ============================================================
// Usamos una ruta más robusta dentro de App_Data para evitar conflictos de permisos
var keysPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "Keys");
if (!Directory.Exists(keysPath)) Directory.CreateDirectory(keysPath);

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysPath))
    .SetApplicationName("HelpDeskSystem");

// NOTA: ProtectedSessionStorage ya se incluye en AddInteractiveServerComponents.

// ============================================================
// 5. CONFIGURACIÓN DE INTERFAZ (BLAZOR Y SIGNALR)
// ============================================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB para adjuntos
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

builder.Services.AddHttpContextAccessor();
var app = builder.Build();

// ============================================================
// 6. INICIALIZACIÓN DE DATOS (SEEDING)
// ============================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        // Al no existir el puente Scoped, usamos la fábrica directamente para el seeding
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = factory.CreateDbContext();

        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await DbInitializer.SeedData(context, configuration);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Fallo al inicializar la base de datos.");
    }
}

// ============================================================
// 7. PIPELINE DE MIDDLEWARE (ORDEN REVISADO)
// ============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// El middleware de Antiforgery debe ir después de archivos estáticos
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();