using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;
using System.Security.Claims;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Web.Auth; // Asegúrate de que el namespace de UserSession sea correcto

namespace HelpDeskSystem.Web.Auth
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        // CORRECCIÓN: Usar el mismo nombre y tipo en todo lado
        private readonly ProtectedSessionStorage _sessionStorage;
        private ClaimsPrincipal _anonymous = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(ProtectedSessionStorage sessionStorage)
        {
            _sessionStorage = sessionStorage;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                // Intentamos leer la sesión
                var result = await _sessionStorage.GetAsync<UserSession>("UserSession");

                if (result.Success && result.Value != null)
                {
                    var userSession = result.Value;
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, userSession.Nombre),
                        new Claim(ClaimTypes.Email, userSession.Email),
                        new Claim(ClaimTypes.Role, userSession.Rol),
                        new Claim(ClaimTypes.Sid, userSession.Id.ToString())
                    };

                    var identity = new ClaimsIdentity(claims, "CustomAuth");
                    var userPrincipal = new ClaimsPrincipal(identity);
                    return new AuthenticationState(userPrincipal);
                }
            }
            catch (InvalidOperationException)
            {
                // ESTE ES EL TRUCO:
                // Si falla porque JS no está listo (común al dar F5), 
                // ignoramos el error y retornamos anónimo temporalmente.
                // Blazor volverá a intentar renderizar cuando se conecte el circuito.
            }
            catch
            {
                // Otros errores
            }

            return new AuthenticationState(_anonymous);
        }
        public async Task MarcarUsuarioComoAutenticado(Usuario usuario)
        {
            // 1. Creamos un objeto de sesión simple para guardar en el navegador
            var userSession = new UserSession
            {
                Id = usuario.Id,
                Nombre = usuario.Nombre,
                Email = usuario.Email,
                Rol = usuario.Rol.ToString() // Importante: Guardar como String
            };

            // 2. GUARDAMOS EN EL NAVEGADOR (Esto es lo que permite que la sesión sobreviva al F5)
            await _sessionStorage.SetAsync("UserSession", userSession);

            // 3. Notificamos a Blazor que el estado de autenticación ha cambiado
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, userSession.Nombre),
                new Claim(ClaimTypes.Email, userSession.Email),
                new Claim(ClaimTypes.Role, userSession.Rol),
                new Claim(ClaimTypes.Sid, userSession.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, "CustomAuth");
            var userPrincipal = new ClaimsPrincipal(identity);

            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(userPrincipal)));
        }

        public async Task MarcarUsuarioComoDesconectado()
        {
            // Borramos la sesión del navegador
            await _sessionStorage.DeleteAsync("UserSession");

            // Notificamos que ahora el usuario es anónimo
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_anonymous)));
        }
    }
}