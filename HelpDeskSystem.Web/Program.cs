using HelpDeskSystem.Data;
using HelpDeskSystem.Web.Components;
using HelpDeskSystem.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using HelpDeskSystem.Web.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authentication.Cookies; // Estándar para evitar Magic Strings

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 1. CONFIGURACIÓN DE INFRAESTRUCTURA (DATOS)
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no fue encontrada.");

// Registro de la Fábrica de Contexto (Optimizado para Blazor Server)
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Bridge para servicios que inyectan el contexto directamente (Compatibilidad)
builder.Services.AddScoped<AppDbContext>(p =>
    p.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

// ============================================================
// 2. SEGURIDAD Y AUTENTICACIÓN (BLINDADO)
// ============================================================

// Eliminamos Magic Strings usando la constante oficial del SDK
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/error";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
        options.SlidingExpiration = true; // Renueva la sesión si el usuario está activo
        options.Cookie.HttpOnly = true;   // Protección contra ataques XSS
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Solo sobre HTTPS
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState(); // Requerido para Blazor 8+
builder.Services.AddControllers();

// Registro del Proveedor de Estado de Autenticación Personalizado
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// ============================================================
// 3. SERVICIOS DE APLICACIÓN (INYECCIÓN DE DEPENDENCIAS)
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
var pathKeys = Path.Combine(builder.Environment.ContentRootPath, "Keys");
if (!Directory.Exists(pathKeys)) Directory.CreateDirectory(pathKeys); // Garantiza que la carpeta exista

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(pathKeys))
    .SetApplicationName("HelpDeskSystem");

builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();

// ============================================================
// 5. CONFIGURACIÓN DE INTERFAZ (BLAZOR Y SIGNALR)
// ============================================================
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB para adjuntos en chat
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

var app = builder.Build();

// ============================================================
// 6. INICIALIZACIÓN DE DATOS (SEEDING)
// ============================================================
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        await DbInitializer.SeedData(context, configuration);
    }
    catch (Exception ex)
    {
        // En un 10/10, los errores de inicio se registran formalmente
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogCritical(ex, "Fallo catastrófico al inicializar la base de datos.");
    }
}

// ============================================================
// 7. PIPELINE DE MIDDLEWARE (EL ORDEN ES CRÍTICO)
// ============================================================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Autenticación SIEMPRE debe ir antes de Autorización
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();