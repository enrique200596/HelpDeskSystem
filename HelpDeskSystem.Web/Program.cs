using HelpDeskSystem.Data;
using HelpDeskSystem.Web.Components;
using HelpDeskSystem.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using HelpDeskSystem.Web.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// 1. Base de Datos
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. Seguridad
builder.Services.AddAuthorization();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
    });

// 3. Servicios de la App
builder.Services.AddScoped<TicketService>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ReportService>();

// 4. Configuración de Sesión
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();

// (AQUÍ BORRAMOS LA LÍNEA AddProtectedBrowserStorage QUE DABA ERROR)
// El servicio ProtectedSessionStorage ya se inyecta automáticamente gracias a la línea de abajo 👇

// 5. Componentes Blazor
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

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

app.Run();