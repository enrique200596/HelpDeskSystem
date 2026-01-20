using HelpDeskSystem.Domain.Entities;

namespace HelpDeskSystem.Web.Services
{
    public interface ITicketService
    {
        // Métodos de Lectura
        Task<List<Ticket>> ObtenerTicketsFiltradosAsync(Guid userId, string rol);
        Task<Ticket?> ObtenerPorIdAsync(int id);
        Task<List<Categoria>> ObtenerCategoriasAsync(bool incluirInactivas = false);

        // Métodos de Escritura
        Task GuardarTicketAsync(Ticket ticket);
        Task ActualizarDescripcionUsuarioAsync(Ticket ticket);
        Task AsignarTicketAsync(int ticketId, Guid asesorId);
        Task ResolverTicketAsync(int id, Guid usuarioEjecutorId);
        Task CalificarTicketAsync(int ticketId, int estrellas, Guid usuarioId);
        Task GuardarCategoriaAsync(Categoria categoria);
        Task ActualizarCategoriaAsync(Categoria categoria);
    }
}