using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface IUsuarioService
    {
        Task<List<Usuario>> ObtenerAsesoresAsync();
    }
}