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

        private void Notificar(int ticketId, string titulo, TipoNotificacion tipo, string? ejecutor, Guid? ownerId, Guid? asesorId)
        {
            // Centraliza la comunicación con el Singleton de notificaciones
            _stateContainer.NotifyStateChanged(ticketId, titulo, tipo, ejecutor, ownerId, asesorId);
        }

        // --- MÉTODOS DE LECTURA (OPTIMIZADOS) ---

        public async Task<List<Categoria>> ObtenerCategoriasAsync(bool incluirInactivas = false)
        {
            using var context = _dbFactory.CreateDbContext();
            var query = context.Categorias.AsNoTracking(); // Mejora rendimiento en lecturas

            if (incluirInactivas)
                query = query.IgnoreQueryFilters();

            return await query.OrderBy(c => c.Nombre).ToListAsync();
        }

        public async Task<List<Ticket>> ObtenerTicketsFiltradosAsync(Guid userId, string rol)
        {
            using var context = _dbFactory.CreateDbContext();

            // Iniciamos la consulta base con las inclusiones necesarias
            var query = context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria)
                .AsNoTracking();

            // Lógica de seguridad por Rol
            if (rol == RolUsuario.Asesor.ToString())
            {
                // CORRECCIÓN: Obtener IDs de categorías asignadas al asesor de forma eficiente
                var idsCategorias = await context.Usuarios
                    .Where(u => u.Id == userId)
                    .SelectMany(u => u.Categorias.Select(c => c.Id))
                    .ToListAsync();

                // Un asesor ve: tickets asignados a él O tickets sin asignar de sus categorías
                query = query.Where(t => t.AsesorId == userId ||
                                   (t.AsesorId == null && idsCategorias.Contains(t.CategoriaId)));
            }
            else if (rol == RolUsuario.Usuario.ToString())
            {
                // Un usuario solo ve sus propios tickets
                query = query.Where(t => t.UsuarioId == userId);
            }
            else if (rol != RolUsuario.Administrador.ToString())
            {
                // Si el rol no es reconocido, no devuelve nada por seguridad
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
            ticket.Estado = EstadoTicket.Abierto; // Estado inicial garantizado

            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            // CORRECCIÓN: Carga explícita del usuario para la notificación
            var usuario = await context.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == ticket.UsuarioId);

            Notificar(ticket.Id, ticket.Titulo, TipoNotificacion.NuevoTicket, usuario?.Nombre, ticket.UsuarioId, null);
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

                var asesor = await context.Usuarios.AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == asesorId);

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

            var ejecutor = await context.Usuarios.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == usuarioEjecutorId);

            Notificar(id, ticket.Titulo, TipoNotificacion.TicketResuelto, ejecutor?.Nombre, ticket.UsuarioId, ticket.AsesorId);
        }

        // Añade estos métodos dentro de la clase TicketService en TicketService.cs
        public async Task ActualizarDescripcionUsuarioAsync(Ticket ticket)
        {
            using var context = _dbFactory.CreateDbContext();
            context.Tickets.Update(ticket);
            await context.SaveChangesAsync();
        }

        public async Task CalificarTicketAsync(int ticketId, int estrellas, Guid usuarioId)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticket = await context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                // Aquí iría tu lógica de calificación (ej. ticket.Estrellas = estrellas)
                await context.SaveChangesAsync();
            }
        }

        public async Task GuardarCategoriaAsync(Categoria categoria)
        {
            using var context = _dbFactory.CreateDbContext();
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