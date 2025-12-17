using HelpDeskSystem.Data;
using HelpDeskSystem.Web.Components;
using HelpDeskSystem.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using HelpDeskSystem.Web.Auth;
using Microsoft.AspNetCore.DataProtection;
// Asegúrate de tener este using para HubOptions:
using Microsoft.AspNetCore.SignalR;

var builder = WebApplication.CreateBuilder(args);

// 1. Base de Datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContextFactory<AppDbContext>(options => options.UseSqlServer(connectionString));

// 2. Seguridad
builder.Services.AddAuthentication("Cookies").AddCookie("Cookies", options => { options.LoginPath = "/login"; options.ExpireTimeSpan = TimeSpan.FromDays(1); });
builder.Services.AddControllers();

// 3. Servicios de la App
builder.Services.AddSingleton<TicketStateContainer>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IManualService, ManualService>(); // <--- Tu nuevo servicio
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// 5. Persistencia de Sesión
var pathKeys = Path.Combine(builder.Environment.ContentRootPath, "Keys");
builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(pathKeys)).SetApplicationName("HelpDeskSystem");
builder.Services.AddScoped<Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();

// 6. Blazor
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// --- 6b. SOLUCIÓN AL ERROR DE TIMEOUT / DESCONEXIÓN ---
// Aumentamos el límite de SignalR para permitir subir fotos y textos largos sin que se caiga.
builder.Services.Configure<HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024; // Aumentar a 10 MB (por defecto es 32KB)
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(30);
});
// -----------------------------------------------------

var app = builder.Build();

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
app.MapControllers();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();