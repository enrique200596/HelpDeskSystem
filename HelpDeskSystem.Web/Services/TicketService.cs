using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class TicketService : ITicketService
    {
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly TicketStateContainer _stateContainer;

        public TicketService(IDbContextFactory<AppDbContext> dbFactory, TicketStateContainer stateContainer)
        {
            _dbFactory = dbFactory;
            _stateContainer = stateContainer;
        }

        /// <summary>
        /// Método privado para centralizar las notificaciones de estado.
        /// Desacopla la lógica de negocio de los mensajes de la interfaz.
        /// </summary>
        private void Notificar(int ticketId, string titulo, TipoNotificacion tipo, string? ejecutor, Guid? ownerId, Guid? asesorId)
        {
            _stateContainer.NotifyStateChanged(ticketId, titulo, tipo, ejecutor, ownerId, asesorId);
        }

        // --- MÉTODOS DE LECTURA ---

        public async Task<List<Categoria>> ObtenerCategoriasAsync(bool incluirInactivas = false)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Categorias.AsQueryable();

            if (incluirInactivas)
                query = query.IgnoreQueryFilters();

            return await query.OrderBy(c => c.Nombre).ToListAsync();
        }

        public async Task<List<Ticket>> ObtenerTicketsFiltradosAsync(Guid userId, string rol)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria)
                .AsNoTracking()
                .AsQueryable();

            if (rol == "Asesor")
            {
                var idsCategorias = await context.Usuarios
                    .Where(u => u.Id == userId)
                    .SelectMany(u => u.Categorias.Select(c => c.Id))
                    .ToListAsync();

                query = query.Where(t => t.AsesorId == userId || (t.AsesorId == null && idsCategorias.Contains(t.CategoriaId)));
            }
            else if (rol == "Usuario")
            {
                query = query.Where(t => t.UsuarioId == userId);
            }
            else if (rol != "Administrador")
            {
                return new List<Ticket>();
            }

            return await query
                .OrderByDescending(t => t.EsUrgente)
                .ThenByDescending(t => t.FechaCreacion)
                .ToListAsync();
        }

        public async Task<Ticket?> ObtenerPorIdAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria)
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        // --- MÉTODOS DE ESCRITURA ---

        public async Task GuardarTicketAsync(Ticket ticket)
        {
            using var context = _dbFactory.CreateDbContext();

            ticket.FechaCreacion = DateTime.UtcNow;
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            var usuario = await context.Usuarios.FindAsync(ticket.UsuarioId);
            Notificar(ticket.Id, ticket.Titulo, TipoNotificacion.NuevoTicket, usuario?.Nombre, ticket.UsuarioId, null);
        }

        public async Task ActualizarDescripcionUsuarioAsync(Ticket ticketModificado)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticketDb = await context.Tickets.FindAsync(ticketModificado.Id);

            if (ticketDb != null)
            {
                ticketDb.Titulo = ticketModificado.Titulo;
                ticketDb.Descripcion = ticketModificado.Descripcion;
                ticketDb.FueEditado = true;

                await context.SaveChangesAsync();

                var usuario = await context.Usuarios.FindAsync(ticketDb.UsuarioId);
                Notificar(ticketDb.Id, ticketDb.Titulo, TipoNotificacion.TicketActualizado, usuario?.Nombre, ticketDb.UsuarioId, ticketDb.AsesorId);
            }
        }

        public async Task AsignarTicketAsync(int ticketId, Guid asesorId)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticket = await context.Tickets.FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket != null)
            {
                ticket.AsesorId = asesorId;
                ticket.Estado = EstadoTicket.Asignado;
                await context.SaveChangesAsync();

                var asesor = await context.Usuarios.FindAsync(asesorId);
                Notificar(ticketId, ticket.Titulo, TipoNotificacion.TicketAsignado, asesor?.Nombre, ticket.UsuarioId, asesorId);
            }
        }

        public async Task ResolverTicketAsync(int id, Guid usuarioEjecutorId)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticket = await context.Tickets.FindAsync(id);

            if (ticket == null || ticket.Estado == EstadoTicket.Resuelto) return;

            ticket.Estado = EstadoTicket.Resuelto;
            ticket.FechaCierre = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var ejecutor = await context.Usuarios.FindAsync(usuarioEjecutorId);
            Notificar(id, ticket.Titulo, TipoNotificacion.TicketResuelto, ejecutor?.Nombre, ticket.UsuarioId, ticket.AsesorId);
        }

        public async Task CalificarTicketAsync(int ticketId, int estrellas, Guid usuarioCalificadorId)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticket = await context.Tickets.FindAsync(ticketId);

            if (ticket != null && ticket.UsuarioId == usuarioCalificadorId)
            {
                ticket.SatisfaccionUsuario = estrellas;
                await context.SaveChangesAsync();

                var usuario = await context.Usuarios.FindAsync(usuarioCalificadorId);
                Notificar(ticketId, ticket.Titulo, TipoNotificacion.NuevaCalificacion, usuario?.Nombre, ticket.UsuarioId, ticket.AsesorId);
            }
        }

        public async Task GuardarCategoriaAsync(Categoria categoria)
        {
            using var context = _dbFactory.CreateDbContext();
            categoria.IsActive = true;
            context.Categorias.Add(categoria);
            await context.SaveChangesAsync();
        }

        public async Task ActualizarCategoriaAsync(Categoria categoria)
        {
            using var context = _dbFactory.CreateDbContext();
            context.Categorias.Update(categoria);
            await context.SaveChangesAsync();
        }
    }
}