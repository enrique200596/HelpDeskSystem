using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    public class TicketService : ITicketService
    {
        // CAMBIO 1: Usamos la Fábrica en lugar del Contexto directo
        private readonly IDbContextFactory<AppDbContext> _dbFactory;
        private readonly TicketStateContainer _stateContainer;

        public TicketService(IDbContextFactory<AppDbContext> dbFactory, TicketStateContainer stateContainer)
        {
            _dbFactory = dbFactory;
            _stateContainer = stateContainer;
        }

        public void NotificarCambio(int? ticketId = null, string? titulo = null, string? remitente = null)
        {
            _stateContainer.NotifyStateChanged(ticketId, titulo, remitente);
        }

        // --- MÉTODOS DE LECTURA (Usan contextos desechables) ---

        public async Task<List<Categoria>> ObtenerCategoriasAsync(bool incluirInactivas = false)
        {
            using var context = _dbFactory.CreateDbContext(); // Creamos contexto nuevo
            var query = context.Categorias.AsQueryable();

            if (!incluirInactivas)
            {
                query = query.Where(c => c.IsActive);
            }
            return await query.ToListAsync();
        }

        public async Task<List<Ticket>> ObtenerTicketsFiltradosAsync(Guid userId, string rol)
        {
            using var context = _dbFactory.CreateDbContext();

            var query = context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria)
                .AsNoTracking() // Importante para rendimiento
                .AsQueryable();

            if (rol == "Administrador") { }
            else if (rol == "Asesor")
            {
                // Obtenemos las categorías del asesor en una subconsulta o lista previa
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
            else
            {
                return new List<Ticket>();
            }

            return await query.OrderByDescending(t => t.EsUrgente)
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
                .AsNoTracking() // ¡CRÍTICO! Esto asegura que veamos cambios (como el nombre del asesor) al instante
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }

        // --- MÉTODOS DE ESCRITURA ---

        public async Task GuardarTicketAsync(Ticket ticket)
        {
            using var context = _dbFactory.CreateDbContext();
            context.Tickets.Add(ticket);
            await context.SaveChangesAsync();

            // Notificar creación
            NotificarCambio(ticket.Id, ticket.Titulo, ticket.Usuario?.Nombre ?? "Nuevo Ticket");
        }

        public async Task ActualizarDescripcionUsuarioAsync(Ticket ticketModificado)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticketDb = await context.Tickets.FindAsync(ticketModificado.Id);

            if (ticketDb != null)
            {
                if (ticketDb.FueEditado) throw new InvalidOperationException("Este ticket ya fue editado una vez.");

                ticketDb.Titulo = ticketModificado.Titulo;
                ticketDb.Descripcion = ticketModificado.Descripcion;
                ticketDb.FueEditado = true;

                await context.SaveChangesAsync();
                NotificarCambio(ticketDb.Id, ticketDb.Titulo, "Actualización");
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

                // Obtenemos nombre del asesor para la notificación
                var asesor = await context.Usuarios.FindAsync(asesorId);
                string nombreAsesor = asesor?.Nombre ?? "Un asesor";

                // Notificar: Esto hará que la pantalla del usuario se recargue y muestre al asesor
                NotificarCambio(ticketId, ticket.Titulo, $"{nombreAsesor} ha tomado tu caso");
            }
        }

        public async Task ResolverTicketAsync(int id, Guid usuarioEjecutorId)
        {
            using var context = _dbFactory.CreateDbContext();
            var ticket = await context.Tickets.FindAsync(id);

            if (ticket == null || ticket.IsDeleted) return;

            // Validación básica (opcional, según reglas)
            // if (ticket.AsesorId != usuarioEjecutorId) ... 

            if (ticket.Estado != EstadoTicket.Resuelto)
            {
                ticket.Estado = EstadoTicket.Resuelto;
                ticket.FechaCierre = DateTime.Now;
                await context.SaveChangesAsync();

                NotificarCambio(id, ticket.Titulo, "El ticket ha sido finalizado");
            }
        }

        public async Task CalificarTicketAsync(int ticketId, int estrellas, Guid usuarioCalificadorId)
        {
            if (estrellas < 1 || estrellas > 5) throw new ArgumentOutOfRangeException("1-5");

            using var context = _dbFactory.CreateDbContext();
            var ticket = await context.Tickets.FindAsync(ticketId);

            if (ticket != null && ticket.UsuarioId == usuarioCalificadorId && ticket.Estado == EstadoTicket.Resuelto)
            {
                ticket.SatisfaccionUsuario = estrellas;
                await context.SaveChangesAsync();

                // Notificamos para que el Admin/Asesor vea la calificación en su tablero
                NotificarCambio(ticketId, ticket.Titulo, "El usuario ha calificado la atención");
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