using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface ITicketService
    {
        // Métodos de Lectura
        Task<List<Ticket>> ObtenerTicketsFiltradosAsync(Guid userId, string rol);
        Task<Ticket?> ObtenerPorIdAsync(int id);
        // Modificamos este para aceptar un filtro opcional
        Task<List<Categoria>> ObtenerCategoriasAsync(bool incluirInactivas = false);
        // Métodos de Escritura
        Task GuardarTicketAsync(Ticket ticket);
        Task ActualizarDescripcionUsuarioAsync(Ticket ticket);
        Task AsignarTicketAsync(int ticketId, Guid asesorId);
        Task ResolverTicketAsync(int id, Guid usuarioEjecutorId);
        Task CalificarTicketAsync(int ticketId, int estrellas, Guid usuarioId);

        // --- NUEVO: Para notificar actualizaciones en tiempo real ---
        void NotificarCambio();

        // NUEVO: Para crear categorías
        Task GuardarCategoriaAsync(Categoria categoria);

        // NUEVO: Para editar o dar de baja
        Task ActualizarCategoriaAsync(Categoria categoria);
    }
}