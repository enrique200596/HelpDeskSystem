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

        public void NotificarCambio(int? ticketId = null, string? titulo = null, string? remitente = null, Guid? ownerId = null, Guid? asesorId = null)
        {
            _stateContainer.NotifyStateChanged(ticketId, titulo, remitente, ownerId, asesorId);
        }

        // --- MÉTODOS DE LECTURA (Usan contextos desechables) ---

        public async Task<List<Categoria>> ObtenerCategoriasAsync(bool incluirInactivas = false)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Categorias.AsQueryable();
            if (!incluirInactivas) query = query.Where(c => c.IsActive);
            return await query.ToListAsync();
        }

        public async Task<List<Ticket>> ObtenerTicketsFiltradosAsync(Guid userId, string rol)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Tickets.Include(t => t.Usuario).Include(t => t.Asesor).Include(t => t.Categoria).AsNoTracking().AsQueryable();

            if (rol == "Administrador") { }
            else if (rol == "Asesor")
            {
                var idsCategorias = await context.Usuarios.Where(u => u.Id == userId).SelectMany(u => u.Categorias.Select(c => c.Id)).ToListAsync();
                query = query.Where(t => t.AsesorId == userId || (t.AsesorId == null && idsCategorias.Contains(t.CategoriaId)));
            }
            else if (rol == "Usuario") { query = query.Where(t => t.UsuarioId == userId); }
            else { return new List<Ticket>(); }

            return await query.OrderByDescending(t => t.EsUrgente).ThenByDescending(t => t.FechaCreacion).ToListAsync();
        }

        public async Task<Ticket?> ObtenerPorIdAsync(int id)
        {
            using var context = _dbFactory.CreateDbContext();
            return await context.Tickets.Include(t => t.Usuario).Include(t => t.Asesor).Include(t => t.Categoria).AsNoTracking().FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }
        // --- ESCRITURA ACTUALIZADA ---

        public async Task GuardarTicketAsync(Ticket ticket)
        {
            using var context = _dbFactory.CreateDbContext();
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            // Notificamos: Owner es el usuario que creó el ticket. Asesor es null.
            NotificarCambio(ticket.Id, ticket.Titulo, ticket.Usuario?.Nombre ?? "Nuevo Ticket", ticket.UsuarioId, null);
        }

        public async Task ActualizarDescripcionUsuarioAsync(Ticket ticketModificado)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticketDb = await context.Tickets.FindAsync(ticketModificado.Id);
            if (ticketDb != null)
            {
                // ... validaciones ...
                ticketDb.Titulo = ticketModificado.Titulo;
                ticketDb.Descripcion = ticketModificado.Descripcion;
                ticketDb.FueEditado = true;
                await context.SaveChangesAsync();

                NotificarCambio(ticketDb.Id, ticketDb.Titulo, "Actualización", ticketDb.UsuarioId, ticketDb.AsesorId);
            }
        }

        public async Task AsignarTicketAsync(int ticketId, Guid asesorId)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticket = await context.Tickets.Include(t => t.Usuario).FirstOrDefaultAsync(t => t.Id == ticketId);
            if (ticket != null)
            {
                ticket.AsesorId = asesorId;
                ticket.Estado = EstadoTicket.Asignado;
                await context.SaveChangesAsync();

                var asesor = await context.Usuarios.FindAsync(asesorId);
                string nombreAsesor = asesor?.Nombre ?? "Un asesor";

                // AVISO IMPORTANTE: Aquí pasamos el AsesorId nuevo para que el Usuario reciba la notificación
                NotificarCambio(ticketId, ticket.Titulo, $"{nombreAsesor} ha tomado tu caso", ticket.UsuarioId, asesorId);
            }
        }

        public async Task ResolverTicketAsync(int id, Guid usuarioEjecutorId)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticket = await context.Tickets.FindAsync(id);
            if (ticket == null || ticket.IsDeleted) return;

            if (ticket.Estado != EstadoTicket.Resuelto)
            {
                ticket.Estado = EstadoTicket.Resuelto;
                ticket.FechaCierre = DateTime.Now;
                await context.SaveChangesAsync();

                NotificarCambio(id, ticket.Titulo, "El ticket ha sido finalizado", ticket.UsuarioId, ticket.AsesorId);
            }
        }

        public async Task CalificarTicketAsync(int ticketId, int estrellas, Guid usuarioCalificadorId)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticket = await context.Tickets.FindAsync(ticketId);
            if (ticket != null && ticket.UsuarioId == usuarioCalificadorId)
            {
                ticket.SatisfaccionUsuario = estrellas;
                await context.SaveChangesAsync();
                NotificarCambio(ticketId, ticket.Titulo, "Nueva Calificación", ticket.UsuarioId, ticket.AsesorId);
            }
        }

        public async Task GuardarCategoriaAsync(Categoria categoria) { using var context = _dbFactory.CreateDbContext(); categoria.IsActive = true; context.Categorias.Add(categoria); await context.SaveChangesAsync(); }
        public async Task ActualizarCategoriaAsync(Categoria categoria) { using var context = _dbFactory.CreateDbContext(); context.Categorias.Update(categoria); await context.SaveChangesAsync(); }
    }
}