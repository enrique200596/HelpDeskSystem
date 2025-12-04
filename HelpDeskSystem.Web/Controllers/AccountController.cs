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
            var usuario = await _authService.LoginAsync(email, password);

            if (usuario == null)
            {
                return Redirect($"/login?error=Credenciales incorrectas&returnUrl={returnUrl}");
            }

            // Crear la identidad del usuario (La Cookie)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
                new Claim(ClaimTypes.Sid, usuario.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, "Cookies");
            var principal = new ClaimsPrincipal(identity);

            // ¡ESTO CREA LA COOKIE ENCRIPTADA!
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