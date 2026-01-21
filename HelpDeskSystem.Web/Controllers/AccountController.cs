using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies; // Capa de estabilidad: Uso de constantes estándar
using System.Security.Claims;
using HelpDeskSystem.Web.Services;
using Microsoft.Extensions.Logging; // Capa de auditoría

namespace HelpDeskSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AccountController> _logger; // Auditoría de accesos

        public AccountController(IAuthService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("/account/login")]
        [ValidateAntiForgeryToken] // Protección 10/10 contra ataques CSRF
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] string returnUrl)
        {
            // Validaciones preventivas de entrada
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Intento de login con campos incompletos desde IP: {IP}", HttpContext.Connection.RemoteIpAddress);
                return Redirect($"/login?error={Uri.EscapeDataString("Todos los campos son obligatorios.")}&returnUrl={returnUrl}");
            }

            // Delegamos la validación lógica al servicio de autenticación
            var (usuario, mensajeError) = await _authService.LoginAsync(email, password);

            if (usuario == null)
            {
                // El log detallado ya ocurre en el AuthService, aquí solo redirigimos
                return Redirect($"/login?error={Uri.EscapeDataString(mensajeError)}&returnUrl={returnUrl}");
            }

            try
            {
                // Configuración de Identidad
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.Nombre),
                    new Claim(ClaimTypes.Email, usuario.Email),
                    new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
                    new Claim(ClaimTypes.Sid, usuario.Id.ToString())
                };

                // MEJORA: Usamos CookieAuthenticationDefaults.AuthenticationScheme en lugar de un string manual
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                // Configuración de persistencia de la cookie
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

                _logger.LogInformation("Sesión iniciada correctamente para el usuario: {Email}", email);

                // SEGURIDAD 10/10: Protección contra Open Redirect
                // Verificamos que la URL de retorno sea local para evitar que redirijan al usuario a sitios maliciosos
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return Redirect("/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error crítico al generar la cookie de autenticación para {Email}", email);
                return Redirect($"/login?error={Uri.EscapeDataString("Error interno al procesar la sesión.")}");
            }
        }

        [HttpPost("/account/logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.FindFirstValue(ClaimTypes.Email);

            // Usamos la constante estándar para cerrar sesión
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            _logger.LogInformation("Sesión cerrada para el usuario: {Email}", userEmail ?? "Desconocido");

            return Redirect("/login");
        }
    }
}