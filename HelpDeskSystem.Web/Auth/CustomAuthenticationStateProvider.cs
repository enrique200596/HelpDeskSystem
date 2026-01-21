using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using HelpDeskSystem.Domain.Entities;
using Microsoft.AspNetCore.Http; // Necesario para IHttpContextAccessor

namespace HelpDeskSystem.Web.Auth
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly IHttpContextAccessor _httpContextAccessor; // Agregado para eficiencia
        private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage, IHttpContextAccessor httpContextAccessor)
        {
            _sessionStorage = sessionStorage;
            _httpContextAccessor = httpContextAccessor;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // 1. INTENTO EFICIENTE: Leer la Cookie del servidor
                // Si el AccountController ya creó la cookie, el HttpContext tendrá el usuario.
                var currentUser = _httpContextAccessor.HttpContext?.User;

                if (currentUser?.Identity != null && currentUser.Identity.IsAuthenticated)
                {
                    return new AuthenticationState(currentUser);
                }

                // 2. RESPALDO: Leer del SessionStorage (para persistencia en el circuito de Blazor)
                var userSessionResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionResult.Success ? userSessionResult.Value : null;

                if (userSession != null)
                {
                    var claimsPrincipal = CreateClaimsPrincipalFromSession(userSession);
                    return new AuthenticationState(claimsPrincipal);
                }

                return new AuthenticationState(_anonymous);
            }
            catch
            {
                return new AuthenticationState(_anonymous);
            }
        }

        private ClaimsPrincipal CreateClaimsPrincipalFromSession(UserSession session)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, session.Nombre),
                new Claim(ClaimTypes.Email, session.Email),
                new Claim(ClaimTypes.Role, session.Rol),
                new Claim(ClaimTypes.Sid, session.Id.ToString())
            }, "CustomAuth"));
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
    }
}