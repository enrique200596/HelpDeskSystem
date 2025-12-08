using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface IUsuarioService
    {
        Task<List<Usuario>> ObtenerAsesoresAsync();
        Task<bool> CrearUsuarioAsync(Usuario usuario, string password);
        Task<Usuario?> ObtenerUsuarioConCategoriasAsync(Guid userId);
        Task ActualizarCategoriasUsuarioAsync(Guid userId, List<int> idsCategoriasSeleccionadas);

        // --- NUEVOS MÉTODOS PARA GESTIÓN ---
        Task<List<Usuario>> ObtenerTodosLosUsuariosAsync();
        Task<bool> ActualizarUsuarioAsync(Usuario usuario);
        Task<bool> CambiarPasswordAsync(Guid userId, string nuevaPasswordPlana);
    }
}