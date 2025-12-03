using HelpDeskSystem.Data;
using HelpDeskSystem.Domain.Entities;
using HelpDeskSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskSystem.Web.Services
{
    // IMPORTANTE: Aquí agregamos ": ITicketService" para cumplir el contrato
    public class TicketService : ITicketService
    {
        private readonly AppDbContext _context;

        public TicketService(AppDbContext context)
        {
            _context = context;
        }

        // --- LECTURA ---

        public async Task<List<Categoria>> ObtenerCategoriasAsync()
        {
            return await _context.Categorias.ToListAsync();
        }

        public async Task<List<Ticket>> ObtenerTicketsFiltradosAsync(Guid userId, string rol)
        {
            var query = _context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria)
                .AsQueryable();

            if (rol == "Administrador")
            {
                // Admin ve todo
            }
            else if (rol == "Asesor")
            {
                var idsCategoriasDelAsesor = await _context.Usuarios
                    .Where(u => u.Id == userId)
                    .SelectMany(u => u.Categorias.Select(c => c.Id))
                    .ToListAsync();

                query = query.Where(t =>
                    t.AsesorId == userId ||
                    (t.AsesorId == null && idsCategoriasDelAsesor.Contains(t.CategoriaId))
                );
            }
            else if (rol == "Usuario")
            {
                query = query.Where(t => t.UsuarioId == userId);
            }
            else
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
            return await _context.Tickets
                .Include(t => t.Usuario)
                .Include(t => t.Asesor)
                .Include(t => t.Categoria)
                .FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted);
        }

        // --- ESCRITURA ---

        public async Task GuardarTicketAsync(Ticket ticket)
        {
            _context.Tickets.Add(ticket);
            await _context.SaveChangesAsync();
        }

        public async Task ActualizarDescripcionUsuarioAsync(Ticket ticketModificado)
        {
            var ticketDb = await _context.Tickets.FindAsync(ticketModificado.Id);
            if (ticketDb != null)
            {
                if (ticketDb.FueEditado)
                    throw new InvalidOperationException("Este ticket ya fue editado una vez.");

                ticketDb.Titulo = ticketModificado.Titulo;
                ticketDb.Descripcion = ticketModificado.Descripcion;
                ticketDb.FueEditado = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task AsignarTicketAsync(int ticketId, Guid asesorId)
        {
            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null)
            {
                ticket.AsesorId = asesorId;
                ticket.Estado = EstadoTicket.Asignado;
                await _context.SaveChangesAsync();
            }
        }

        public async Task ResolverTicketAsync(int id, Guid usuarioEjecutorId)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null || ticket.IsDeleted) return;

            if (ticket.AsesorId != usuarioEjecutorId)
            {
                throw new UnauthorizedAccessException("Solo el asesor asignado puede resolver.");
            }

            if (ticket.Estado != EstadoTicket.Resuelto)
            {
                ticket.Estado = EstadoTicket.Resuelto;
                ticket.FechaCierre = DateTime.Now; // Importante para reportes
                await _context.SaveChangesAsync();
            }
        }

        public async Task CalificarTicketAsync(int ticketId, int estrellas, Guid usuarioCalificadorId)
        {
            if (estrellas < 1 || estrellas > 5) throw new ArgumentOutOfRangeException("1-5");

            var ticket = await _context.Tickets.FindAsync(ticketId);
            if (ticket != null && ticket.UsuarioId == usuarioCalificadorId && ticket.Estado == EstadoTicket.Resuelto)
            {
                ticket.SatisfaccionUsuario = estrellas;
                await _context.SaveChangesAsync();
            }
        }
    }
}