using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface IUsuarioService
    {
        Task<List<Usuario>> ObtenerAsesoresAsync();
        Task<bool> CrearUsuarioAsync(Usuario usuario, string password); // Del paso anterior

        // --- NUEVOS MÉTODOS ---
        Task<Usuario?> ObtenerUsuarioConCategoriasAsync(Guid userId);
        Task ActualizarCategoriasUsuarioAsync(Guid userId, List<int> idsCategoriasSeleccionadas);
    }
}