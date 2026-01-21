using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface IAuthService
    {
        /// <summary>
        /// Procesa el inicio de sesión de un usuario.
        /// Retorna una tupla con el Usuario (si tiene éxito) y un mensaje de error amigable.
        /// </summary>
        Task<(Usuario? Usuario, string MensajeError)> LoginAsync(string email, string password);
    }
}