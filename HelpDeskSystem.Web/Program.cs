using HelpDeskSystem.Data;
using HelpDeskSystem.Web.Components;
using HelpDeskSystem.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using HelpDeskSystem.Web.Auth;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuración de Base de Datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Registrar la Fábrica (Singleton) para Blazor
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// Bridge para servicios Scoped que requieren AppDbContext directamente
builder.Services.AddScoped<AppDbContext>(p =>
    p.GetRequiredService<IDbContextFactory<AppDbContext>>().CreateDbContext());

// 2. Seguridad y Autenticación
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromDays(1);
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// 3. Servicios de Aplicación (Inyección de Dependencias)
builder.Services.AddSingleton<TicketStateContainer>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IManualService, ManualService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 4. Persistencia de Sesión y Protección de Datos
var pathKeys = Path.Combine(builder.Environment.ContentRootPath, "Keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(pathKeys))
    .SetApplicationName("HelpDeskSystem");

builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();

// 5. Configuración de Blazor y SignalR
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.Configure<HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // 10 MB para adjuntos
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// 6. Inicialización de Datos (Seeding)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        var configuration = services.GetRequiredService<IConfiguration>();
        // Llamada asíncrona para asegurar que el Admin exista
        await DbInitializer.SeedData(context, configuration);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error crítico en la inicialización: {ex.Message}");
    }
}

// 7. Pipeline de Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers(); // Necesario para UploadController
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();