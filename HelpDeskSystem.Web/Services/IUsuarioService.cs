using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface IUsuarioService
    {
        Task<List<Usuario>> ObtenerAsesoresAsync();
        Task<bool> CrearUsuarioAsync(Usuario usuario, string password);
        Task<Usuario?> ObtenerUsuarioConCategoriasAsync(Guid userId);
        Task ActualizarCategoriasUsuarioAsync(Guid userId, List<int> idsCategoriasSeleccionadas);

        // --- MÉTODOS PARA GESTIÓN ---
        Task<List<Usuario>> ObtenerTodosLosUsuariosAsync();
        Task<bool> ActualizarUsuarioAsync(Usuario usuarioModificado);
        Task<bool> CambiarPasswordAsync(Guid userId, string nuevaPasswordPlana);
        Task<Usuario?> ObtenerPorEmailAsync(string email);
    }
}