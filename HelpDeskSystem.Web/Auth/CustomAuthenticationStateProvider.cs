using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using HelpDeskSystem.Domain.Entities; // Necesario para la clase Usuario

namespace HelpDeskSystem.Web.Auth
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private readonly ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        // --- MÉTODO BLINDADO PARA LEER SESIÓN (CORREGIDO) ---
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // El try-catch aquí es VITAL. Evita que la app explote si se llama 
                // antes de que el navegador esté conectado (prerendering).
                var userSessionResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionResult.Success ? userSessionResult.Value : null;

                if (userSession == null)
                    return await Task.FromResult(new AuthenticationState(_anonymous));

                // Aquí usamos las propiedades correctas de tu clase (Nombre, Email, Rol, Id)
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.Nombre),
                    new Claim(ClaimTypes.Email, userSession.Email),
                    new Claim(ClaimTypes.Role, userSession.Rol),
                    new Claim(ClaimTypes.Sid, userSession.Id.ToString())
                }, "CustomAuth"));

                return await Task.FromResult(new AuthenticationState(claimsPrincipal));
            }
            catch
            {
                // Si ocurre cualquier error de conexión o lectura, devolvemos anónimo
                // en lugar de lanzar la pantalla amarilla de la muerte.
                return await Task.FromResult(new AuthenticationState(_anonymous));
            }
        }

        // --- MANTENEMOS ESTE NOMBRE PARA QUE NO FALLE EL LOGIN ---
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

            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.Nombre),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.Rol),
                new Claim(ClaimTypes.Sid, userSession.Id.ToString())
            }, "CustomAuth"));

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        // --- MANTENEMOS ESTE NOMBRE PARA QUE NO FALLE EL LOGOUT ---
        public async Task MarcarUsuarioComoDesconectado()
        {
            await _sessionStorage.DeleteAsync("UserSession");
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }
    }
}