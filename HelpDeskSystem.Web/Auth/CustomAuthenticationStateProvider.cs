using System.Security.Claims;
using HelpDeskSystem.Domain.Entities;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace HelpDeskSystem.Web.Auth
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedSessionStorage _sessionStorage;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        // ESTE MÉTODO SE EJECUTA CADA VEZ QUE APRETAS F5 O CAMBIAS DE PÁGINA
        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // 1. Buscamos si hay datos guardados en el navegador
                var userSessionStorageResult = await _sessionStorage.GetAsync<UserSession>("UserSession");
                var userSession = userSessionStorageResult.Success ? userSessionStorageResult.Value : null;

                // 2. Si no hay datos, retornamos "Anónimo"
                if (userSession == null)
                    return await Task.FromResult(new AuthenticationState(_anonymous));

                // 3. Si hay datos, reconstruimos el usuario (Claims)
                var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userSession.Nombre),
                    new Claim(ClaimTypes.Email, userSession.Email),
                    new Claim(ClaimTypes.Role, userSession.Rol),
                    new Claim(ClaimTypes.Sid, userSession.Id)
                }, "CustomAuth"));

                return await Task.FromResult(new AuthenticationState(claimsPrincipal));
            }
            catch
            {
                // Si ocurre un error leyendo el navegador, retornamos anónimo por seguridad
                return await Task.FromResult(new AuthenticationState(_anonymous));
            }
        }

        // --- LOGIN ---
        public async Task MarcarUsuarioComoAutenticado(Usuario usuario)
        {
            // 1. Guardamos los datos en el navegador (Persistencia)
            var userSession = new UserSession
            {
                Id = usuario.Id.ToString(),
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString()
            };

            await _sessionStorage.SetAsync("UserSession", userSession);

            // 2. Avisamos a Blazor que se actualice ya mismo
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Nombre),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol.ToString()),
                new Claim(ClaimTypes.Sid, usuario.Id.ToString())
            }, "CustomAuth"));

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(claimsPrincipal)));
        }

        // --- LOGOUT ---
        public async Task MarcarUsuarioComoDesconectado()
        {
            // Borramos los datos del navegador
            await _sessionStorage.DeleteAsync("UserSession");

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }
    }
}