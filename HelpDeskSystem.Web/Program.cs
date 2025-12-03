using HelpDeskSystem.Data;
using HelpDeskSystem.Web.Components;
using HelpDeskSystem.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using HelpDeskSystem.Web.Auth;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// 1. Base de Datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// 2. Seguridad (SOLO Authorization, sin Authentication de Cookies)
builder.Services.AddAuthorization();
// ¡OJO! Aquí eliminamos AddAuthentication(...).AddCookie(...)

// 3. Servicios de la App
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 4. Proveedor de Autenticación Personalizado
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// 5. Persistencia de Sesión (Para F5)
var pathKeys = Path.Combine(builder.Environment.ContentRootPath, "Keys");
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(pathKeys))
    .SetApplicationName("HelpDeskSystem");

// Servicio necesario para leer el navegador
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();

// 6. Blazor
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// 7. Middleware
// ¡OJO! Eliminamos app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();