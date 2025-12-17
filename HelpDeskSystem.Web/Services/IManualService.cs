using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface IManualService
    {
        // Ahora recibe el rol opcionalmente
        Task<List<Manual>> ObtenerTodosAsync(string? rolUsuario = null);

        Task<Manual?> ObtenerPorIdAsync(int id);

        // Ahora pide el ID del usuario que edita
        Task GuardarManualAsync(Manual manual, Guid usuarioEditorId);

        // Ahora pide quién borra y si es admin
        Task EliminarManualAsync(int id, Guid usuarioId, bool esAdmin);

        // Nuevo método para historial
        Task<List<ManualLog>> ObtenerHistorialAsync(int manualId);
    }
}