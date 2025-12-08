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

// ❌ BORRA o COMENTA la línea antigua:
//builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(connectionString));

// ✅ AGREGA ESTO:
builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(connectionString)); // O la base de datos que uses

// 2. Seguridad: Agregar soporte para Cookies (Correcto)
builder.Services.AddAuthentication("Cookies").AddCookie("Cookies", options => { options.LoginPath = "/login"; options.ExpireTimeSpan = TimeSpan.FromDays(1); });

// --- ¡FALTA ESTO! Agrega soporte para Controladores (necesario para AccountController) ---
builder.Services.AddControllers();
// ---------------------------------------------------------------------------------------

// 3. Servicios de la App
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 4. Proveedor de Autenticación: Comentado CORRECTAMENTE (Blazor usará el de defecto)
// builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// 5. Persistencia de Sesión (Para F5)
var pathKeys = Path.Combine(builder.Environment.ContentRootPath, "Keys");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(pathKeys)).SetApplicationName("HelpDeskSystem");

// Servicio necesario para leer el navegador (Opcional si ya no lo usas, pero no estorba)
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();

// 6. Blazor
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// 7. Middleware (Orden Correcto)
app.UseAuthentication();
app.UseAuthorization();

// --- ¡FALTA ESTO! Activa las rutas de los controladores ---
app.MapControllers();
// ---------------------------------------------------------

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();