using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using HelpDeskSystem.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace HelpDeskSystem.Web.Auth
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage, IHttpContextAccessor httpContextAccessor)
        {
            _sessionStorage = sessionStorage;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // 1. FUENTE DE VERDAD PRIMARIA: La Cookie de Autenticación.
                // Durante el renderizado inicial (SSR), el HttpContext contiene al usuario autenticado por el AccountController.
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext?.User?.Identity != null && httpContext.User.Identity.IsAuthenticated)
                {
                    return new AuthenticationState(httpContext.User);
                }

                // 2. FUENTE DE RESPALDO: SessionStorage.
                // Una vez que Blazor es interactivo (SignalR), el HttpContext puede ser nulo o perderse.
                // Usamos SessionStorage para mantener al usuario dentro del circuito actual.
                var userSessionResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionResult.Success ? userSessionResult.Value : null;

                if (userSession != null)
                {
                    var claimsPrincipal = CreateClaimsPrincipalFromSession(userSession);
                    return new AuthenticationState(claimsPrincipal);
                }
            }
            catch
            {
                // Captura excepciones de JS Interop durante el pre-renderizado (cuando SessionStorage no es accesible).
                return new AuthenticationState(_anonymous);
            }

            return new AuthenticationState(_anonymous);
        }

        public async Task MarcarUsuarioComoAutenticado(Usuario usuario)
        {
            var userSession = new UserSession
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString()
            };

            await _sessionStorage.SetAsync("UserSession", userSession);
            var claimsPrincipal = CreateClaimsPrincipalFromSession(userSession);
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        public async Task MarcarUsuarioComoDesconectado()
        {
            await _sessionStorage.DeleteAsync("UserSession");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }

        private ClaimsPrincipal CreateClaimsPrincipalFromSession(UserSession session)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.Name, session.Nombre),
                new(ClaimTypes.Email, session.Email),
                new(ClaimTypes.Role, session.Rol),
                new(ClaimTypes.Sid, session.Id.ToString())
            };

            // CORRECCIÓN: Usamos CookieAuthenticationDefaults.AuthenticationScheme para que el sistema
            // reconozca la identidad como válida y coincida con el AccountController.
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            return new ClaimsPrincipal(identity);
        }
    }
}