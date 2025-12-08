using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using HelpDeskSystem.Web.Services;

namespace HelpDeskSystem.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("/account/login")]
        public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] string returnUrl)
        {
            // Desestructuramos la respuesta del servicio
            var (usuario, mensajeError) = await _authService.LoginAsync(email, password);

            // Si el usuario es nulo, redirigimos con el mensaje específico
            if (usuario == null)
            {
                // Usamos Uri.EscapeDataString para asegurar que el mensaje viaje bien por la URL
                return Redirect($"/login?error={Uri.EscapeDataString(mensajeError)}&returnUrl={returnUrl}");
            }

            // --- Lógica de éxito (Crear Cookie) ---
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
                new Claim(ClaimTypes.Sid, usuario.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("Cookies", principal);

            return Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }

        [HttpGet("/account/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return Redirect("/login");
        }
    }
}