using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface IAuthService
    {
        // Cambiamos el retorno a una tupla (Usuario?, string)
        Task<(Usuario? Usuario, string MensajeError)> LoginAsync(string email, string password);
    }
}